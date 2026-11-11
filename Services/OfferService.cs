using ARTISTO.Data;
using ARTISTO.Models;

namespace ARTISTO.Services;

public class OfferService(IOfferRepository offers) : IOfferService
{
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

        var id = await offers.TryInsertAsync(offer, cancellationToken);
        if (id is null)
        {
            return await GetEligibilityErrorAsync(request.ArtworkListingId, buyerId, cancellationToken)
                ?? ServiceResult<OfferResponse>.Conflict("You already have a pending offer for this artwork.");
        }

        return ServiceResult<OfferResponse>.Success(new OfferResponse(
            id.Value, offer.ArtworkListingId, offer.BuyerId, offer.Amount, offer.Message, offer.Status, offer.CreatedAtUtc));
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

        if (!listing.IsAvailable)
        {
            return ServiceResult<OfferResponse>.Conflict("This artwork is no longer available.");
        }

        return null;
    }
}
