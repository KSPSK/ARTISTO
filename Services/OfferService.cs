using ARTISTO.Data;
using ARTISTO.Models;
using Microsoft.EntityFrameworkCore;

namespace ARTISTO.Services;

public class OfferService(
    IOfferRepository offers,
    IOrderService orders,
    INotificationService notifications,
    ArtistoDbContext db) : IOfferService
{
    public async Task<ServiceResult<List<OfferResponse>>> GetMyOffersAsync(int userId, string? role)
    {
        var trimmedRole = role?.Trim();

        if (!string.IsNullOrEmpty(trimmedRole)
            && !string.Equals(trimmedRole, "buyer", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(trimmedRole, "seller", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult<List<OfferResponse>>.BadRequest("Role must be either 'buyer' or 'seller'.");
        }

        var list = await offers.GetOffersForUserAsync(userId, trimmedRole);
        return ServiceResult<List<OfferResponse>>.Success(list.Select(ToResponse).ToList());
    }

    public async Task<ServiceResult<OfferResponse>> GetByIdAsync(int id, int userId)
    {
        var offer = await offers.GetByIdAsync(id);
        if (offer is null)
        {
            return ServiceResult<OfferResponse>.NotFound($"Offer {id} was not found.");
        }

        if (offer.BuyerId != userId && offer.ArtworkListing?.UserId != userId)
        {
            return ServiceResult<OfferResponse>.Forbidden();
        }

        return ServiceResult<OfferResponse>.Success(ToResponse(offer));
    }

    public async Task<ServiceResult<OfferResponse>> CreateOfferAsync(
        CreateOfferRequest request, int buyerId, CancellationToken cancellationToken)
    {
        if (decimal.Round(request.Amount, 2) != request.Amount)
        {
            return ServiceResult<OfferResponse>.BadRequest("The offer amount can have at most two decimal places.");
        }

        var eligibilityError = await GetEligibilityErrorAsync(request.ArtworkListingId, buyerId, cancellationToken);
        if (eligibilityError is not null)
        {
            return eligibilityError;
        }

        var offer = new Offer
        {
            ArtworkListingId = request.ArtworkListingId,
            BuyerId = buyerId,
            Amount = request.Amount,
            Message = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim()
        };

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.LockListingForPurchaseAsync(request.ArtworkListingId, cancellationToken);

        var id = await offers.TryInsertAsync(offer, cancellationToken);
        if (id is null)
        {
            return await GetEligibilityErrorAsync(request.ArtworkListingId, buyerId, cancellationToken)
                ?? ServiceResult<OfferResponse>.Conflict("You already have a pending offer for this artwork.");
        }

        // A purchase may have reserved the artwork while this offer was being saved.
        // If so, decline it right away, so an offer never stays open on an artwork that is not available.
        if (await offers.DeclineIfListingUnavailableAsync(id.Value))
        {
            return ServiceResult<OfferResponse>.Conflict("This artwork is no longer available.");
        }

        var created = await offers.GetByIdAsync(id.Value);
        if (created is null)
        {
            return ServiceResult<OfferResponse>.NotFound($"Offer {id.Value} was not found.");
        }

        var response = ToResponse(created);

        if (response.SellerId is int sellerId)
        {
            await notifications.NotifyAsync(
                sellerId,
                NotificationType.OfferReceived,
                $"{response.BuyerDisplayName} offered {response.Amount} EUR for \"{response.ArtworkTitle}\".",
                "/offers");
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ServiceResult<OfferResponse>.Success(response);
    }

    public async Task<ServiceResult<OfferResponse>> AcceptOfferAsync(int id, int userId)
    {
        var offer = await offers.GetByIdForUpdateAsync(id);
        if (offer is null)
        {
            return ServiceResult<OfferResponse>.NotFound($"Offer {id} was not found.");
        }

        var sellerId = offer.ArtworkListing?.UserId;
        decimal agreedPrice;

        if (offer.Status == OfferStatus.Pending)
        {
            if (sellerId != userId)
            {
                return ServiceResult<OfferResponse>.Forbidden("Only the seller can accept this offer.");
            }

            agreedPrice = offer.Amount;
        }
        else if (offer.Status == OfferStatus.Countered)
        {
            if (offer.BuyerId != userId)
            {
                return ServiceResult<OfferResponse>.Forbidden("Only the buyer can accept the counter-offer.");
            }

            agreedPrice = offer.CounterAmount!.Value;
        }
        else
        {
            return ServiceResult<OfferResponse>.BadRequest(
                $"This offer is already {offer.Status.ToString().ToLowerInvariant()}.");
        }

        // Save the order, offer changes and notifications in one transaction.
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.LockListingForPurchaseAsync(offer.ArtworkListingId);

        offer.Status = OfferStatus.Accepted;
        offer.RespondedAtUtc = DateTime.UtcNow;

        var otherOffers = await offers.GetOpenOffersForListingAsync(offer.ArtworkListingId, offer.Id);
        foreach (var other in otherOffers)
        {
            other.Status = OfferStatus.Declined;
            other.RespondedAtUtc = DateTime.UtcNow;
        }

        var orderResult = await orders.CreateOrderFromOfferAsync(offer.ArtworkListingId, offer.BuyerId, agreedPrice);
        if (!orderResult.IsSuccess)
        {
            return new ServiceResult<OfferResponse>(default, orderResult.Status, orderResult.ErrorMessage);
        }

        offer.OrderId = orderResult.Value!.Id;

        // Offers submitted after otherOffers was loaded are declined now that the artwork is reserved.
        var lateBuyerIds = await offers.DeclineOpenOffersIfListingUnavailableAsync(offer.ArtworkListingId, offer.Id);

        var otherPartyId = userId == offer.BuyerId ? sellerId : offer.BuyerId;
        if (otherPartyId is int notifyUserId)
        {
            await notifications.NotifyAsync(
                notifyUserId,
                NotificationType.OfferAccepted,
                $"The offer for \"{offer.ArtworkListing?.Title}\" was accepted. An order for {agreedPrice} EUR was created.",
                $"/orders/{offer.OrderId}");
        }

        var declinedBuyerIds = otherOffers
            .Select(other => other.BuyerId)
            .Concat(lateBuyerIds)
            .Distinct();

        foreach (var declinedBuyerId in declinedBuyerIds)
        {
            await notifications.NotifyAsync(
                declinedBuyerId,
                NotificationType.OfferDeclined,
                $"Your offer for \"{offer.ArtworkListing?.Title}\" was declined because the artwork was sold to another buyer.",
                "/offers");
        }

        if (!await TrySaveAsync())
        {
            return ServiceResult<OfferResponse>.Conflict(OfferChangedMessage);
        }

        await transaction.CommitAsync();

        return ServiceResult<OfferResponse>.Success(ToResponse(offer));
    }

    public async Task<ServiceResult<OfferResponse>> DeclineOfferAsync(int id, int userId)
    {
        var offer = await offers.GetByIdForUpdateAsync(id);
        if (offer is null)
        {
            return ServiceResult<OfferResponse>.NotFound($"Offer {id} was not found.");
        }

        var sellerId = offer.ArtworkListing?.UserId;

        if (offer.Status == OfferStatus.Pending)
        {
            if (sellerId != userId)
            {
                return ServiceResult<OfferResponse>.Forbidden("Only the seller can decline this offer.");
            }
        }
        else if (offer.Status == OfferStatus.Countered)
        {
            if (offer.BuyerId != userId)
            {
                return ServiceResult<OfferResponse>.Forbidden("Only the buyer can decline the counter-offer.");
            }
        }
        else
        {
            return ServiceResult<OfferResponse>.BadRequest(
                $"This offer is already {offer.Status.ToString().ToLowerInvariant()}.");
        }

        offer.Status = OfferStatus.Declined;
        offer.RespondedAtUtc = DateTime.UtcNow;

        var otherPartyId = userId == offer.BuyerId ? sellerId : offer.BuyerId;
        if (otherPartyId is int notifyUserId)
        {
            await notifications.NotifyAsync(
                notifyUserId,
                NotificationType.OfferDeclined,
                $"The offer for \"{offer.ArtworkListing?.Title}\" was declined.",
                "/offers");
        }

        if (!await TrySaveAsync())
        {
            return ServiceResult<OfferResponse>.Conflict(OfferChangedMessage);
        }

        return ServiceResult<OfferResponse>.Success(ToResponse(offer));
    }

    public async Task<ServiceResult<OfferResponse>> CounterOfferAsync(int id, CounterOfferRequest request, int userId)
    {
        var offer = await offers.GetByIdForUpdateAsync(id);
        if (offer is null)
        {
            return ServiceResult<OfferResponse>.NotFound($"Offer {id} was not found.");
        }

        var listing = offer.ArtworkListing;
        if (listing is null || listing.UserId != userId)
        {
            return ServiceResult<OfferResponse>.Forbidden("Only the seller can make a counter-offer.");
        }

        if (offer.Status != OfferStatus.Pending)
        {
            return ServiceResult<OfferResponse>.BadRequest("Only pending offers can be countered.");
        }

        if (!listing.IsAvailable)
        {
            return ServiceResult<OfferResponse>.Conflict("This artwork is no longer available.");
        }

        if (decimal.Round(request.Amount, 2) != request.Amount)
        {
            return ServiceResult<OfferResponse>.BadRequest("The counter-offer amount can have at most two decimal places.");
        }

        if (request.Amount <= offer.Amount)
        {
            return ServiceResult<OfferResponse>.BadRequest("The counter-offer must be higher than the buyer's offer.");
        }

        if (request.Amount > listing.Price)
        {
            return ServiceResult<OfferResponse>.BadRequest("The counter-offer cannot be higher than the listing price.");
        }

        offer.Status = OfferStatus.Countered;
        offer.CounterAmount = request.Amount;
        offer.CounterMessage = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim();
        offer.RespondedAtUtc = DateTime.UtcNow;

        await notifications.NotifyAsync(
            offer.BuyerId,
            NotificationType.OfferCountered,
            $"The seller countered your offer for \"{listing.Title}\" with {request.Amount} EUR.",
            "/offers");

        if (!await TrySaveAsync())
        {
            return ServiceResult<OfferResponse>.Conflict(OfferChangedMessage);
        }

        return ServiceResult<OfferResponse>.Success(ToResponse(offer));
    }

    private async Task<ServiceResult<OfferResponse>?> GetEligibilityErrorAsync(
        int listingId, int buyerId, CancellationToken cancellationToken)
    {
        if (!await offers.BuyerExistsAsync(buyerId, cancellationToken))
        {
            return ServiceResult<OfferResponse>.Unauthorized("A valid user account is required to make an offer.");
        }

        var listing = await offers.GetListingAsync(listingId, cancellationToken);
        if (listing is null)
        {
            return ServiceResult<OfferResponse>.NotFound("Artwork listing not found.");
        }

        if (listing.UserId is null)
        {
            return ServiceResult<OfferResponse>.BadRequest("This artwork does not have a seller.");
        }

        if (listing.UserId == buyerId)
        {
            return ServiceResult<OfferResponse>.Forbidden("You cannot make an offer on your own artwork.");
        }

        if (!listing.LocalPickupAvailable)
        {
            return ServiceResult<OfferResponse>.BadRequest("This artwork listing does not offer local pickup.");
        }

        if (!listing.IsAvailable)
        {
            return ServiceResult<OfferResponse>.Conflict("This artwork is no longer available.");
        }

        return null;
    }

    private const string OfferChangedMessage =
        "This offer was changed by someone else at the same time. Refresh it and try again.";

    // Returns false when another request changed the same offer first.
    private async Task<bool> TrySaveAsync()
    {
        try
        {
            await offers.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    private static OfferResponse ToResponse(Offer offer)
    {
        return new OfferResponse
        {
            Id = offer.Id,
            ArtworkListingId = offer.ArtworkListingId,
            ArtworkTitle = offer.ArtworkListing?.Title ?? string.Empty,
            ArtworkImageUrl = offer.ArtworkListing?.ImageUrl,
            ListingPrice = offer.ArtworkListing?.Price ?? 0,
            BuyerId = offer.BuyerId,
            BuyerDisplayName = offer.Buyer?.DisplayName ?? string.Empty,
            SellerId = offer.ArtworkListing?.UserId,
            SellerDisplayName = offer.ArtworkListing?.User?.DisplayName ?? string.Empty,
            Amount = offer.Amount,
            Message = offer.Message,
            Status = offer.Status,
            CounterAmount = offer.CounterAmount,
            CounterMessage = offer.CounterMessage,
            OrderId = offer.OrderId,
            CreatedAtUtc = offer.CreatedAtUtc,
            RespondedAtUtc = offer.RespondedAtUtc
        };
    }
}
