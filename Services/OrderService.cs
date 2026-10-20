using ARTISTO.Data;
using ARTISTO.Models;
using Microsoft.EntityFrameworkCore;

namespace ARTISTO.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;

    public OrderService(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<ServiceResult<List<OrderResponse>>> GetMyOrdersAsync(int userId, string? role)
    {
        var trimmedRole = role?.Trim();

        if (!string.IsNullOrEmpty(trimmedRole)
            && !string.Equals(trimmedRole, "buyer", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(trimmedRole, "seller", StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult<List<OrderResponse>>.BadRequest("Role must be either 'buyer' or 'seller'.");
        }

        var orders = await _orderRepository.GetOrdersForUserAsync(userId, trimmedRole);
        return ServiceResult<List<OrderResponse>>.Success(orders.Select(ToResponse).ToList());
    }

    public async Task<ServiceResult<OrderResponse>> GetByIdAsync(int id, int userId)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order is null)
        {
            return ServiceResult<OrderResponse>.NotFound($"Order {id} was not found.");
        }

        if (order.BuyerId != userId && order.SellerId != userId)
        {
            return ServiceResult<OrderResponse>.Forbidden();
        }

        return ServiceResult<OrderResponse>.Success(ToResponse(order));
    }

    public async Task<ServiceResult<OrderResponse>> CreateOrderAsync(CreateOrderRequest request, int buyerId)
    {
        if (!await _orderRepository.UserExistsAsync(buyerId))
        {
            return ServiceResult<OrderResponse>.Unauthorized("A valid user account is required to place an order.");
        }

        var listing = await _orderRepository.GetListingWithUserAsync(request.ArtworkListingId);
        if (listing is null)
        {
            return ServiceResult<OrderResponse>.NotFound($"Artwork listing {request.ArtworkListingId} was not found.");
        }

        if (listing.UserId is null || listing.User is null)
        {
            return ServiceResult<OrderResponse>.BadRequest("This artwork listing has no seller and cannot be ordered.");
        }

        if (listing.UserId == buyerId)
        {
            return ServiceResult<OrderResponse>.BadRequest("You cannot order your own artwork listing.");
        }

        if (!listing.LocalPickupAvailable)
        {
            return ServiceResult<OrderResponse>.BadRequest("This artwork listing does not offer local pickup and cannot be ordered.");
        }

        if (listing.Status != ArtworkListingStatus.Available)
        {
            return ServiceResult<OrderResponse>.Conflict(
                $"This artwork listing is currently {listing.Status.ToString().ToLowerInvariant()} and cannot be ordered.");
        }

        var buyer = await _orderRepository.GetUserByIdAsync(buyerId);
        if (buyer is null)
        {
            return ServiceResult<OrderResponse>.Unauthorized("Buyer account not found.");
        }

        var sellerId = listing.UserId.Value;
        var seller = listing.User;

        var order = new Order
        {
            ArtworkListingId = listing.Id,
            ArtworkListing = listing,
            BuyerId = buyerId,
            Buyer = buyer,
            SellerId = sellerId,
            Seller = seller,
            Price = listing.Price,
            Status = OrderStatus.Pending,
            PickupNotes = request.PickupNotes?.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        listing.Status = ArtworkListingStatus.Reserved;

        await _orderRepository.AddAsync(order);

        try
        {
            await _orderRepository.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult<OrderResponse>.Conflict(
                "This artwork listing was already reserved by another concurrent order.");
        }

        return ServiceResult<OrderResponse>.Success(ToResponse(order));
    }

    public async Task<ServiceResult<OrderResponse>> ConfirmHandoverAsync(int id, int sellerId)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order is null)
        {
            return ServiceResult<OrderResponse>.NotFound($"Order {id} was not found.");
        }

        if (order.SellerId != sellerId)
        {
            return ServiceResult<OrderResponse>.Forbidden();
        }

        if (order.Status != OrderStatus.Pending)
        {
            return ServiceResult<OrderResponse>.BadRequest(
                $"Handover can only be confirmed for pending orders. Current status: {order.Status}.");
        }

        order.Status = OrderStatus.HandedOver;
        order.HandedOverAtUtc = DateTime.UtcNow;

        await _orderRepository.SaveChangesAsync();
        return ServiceResult<OrderResponse>.Success(ToResponse(order));
    }

    public async Task<ServiceResult<OrderResponse>> ConfirmReceiptAsync(int id, int buyerId)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order is null)
        {
            return ServiceResult<OrderResponse>.NotFound($"Order {id} was not found.");
        }

        if (order.BuyerId != buyerId)
        {
            return ServiceResult<OrderResponse>.Forbidden();
        }

        if (order.Status != OrderStatus.HandedOver)
        {
            return ServiceResult<OrderResponse>.BadRequest(
                $"Order receipt can only be confirmed after the seller has handed it over. Current status: {order.Status}.");
        }

        order.Status = OrderStatus.Completed;
        order.CompletedAtUtc = DateTime.UtcNow;

        if (order.ArtworkListing is not null)
        {
            order.ArtworkListing.Status = ArtworkListingStatus.Sold;
        }

        await _orderRepository.SaveChangesAsync();
        return ServiceResult<OrderResponse>.Success(ToResponse(order));
    }

    public async Task<ServiceResult<OrderResponse>> CancelOrderAsync(int id, CancelOrderRequest? request, int userId)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order is null)
        {
            return ServiceResult<OrderResponse>.NotFound($"Order {id} was not found.");
        }

        if (order.BuyerId != userId && order.SellerId != userId)
        {
            return ServiceResult<OrderResponse>.Forbidden();
        }

        if (order.Status != OrderStatus.Pending)
        {
            return ServiceResult<OrderResponse>.BadRequest(
                $"Only pending orders can be cancelled. Current status is '{order.Status}'.");
        }

        order.Status = OrderStatus.Cancelled;
        order.CancelledAtUtc = DateTime.UtcNow;
        order.CancellationReason = string.IsNullOrWhiteSpace(request?.Reason)
            ? null
            : request.Reason.Trim();

        if (order.ArtworkListing is not null)
        {
            order.ArtworkListing.Status = ArtworkListingStatus.Available;
        }

        await _orderRepository.SaveChangesAsync();
        return ServiceResult<OrderResponse>.Success(ToResponse(order));
    }

    public static OrderResponse ToResponse(Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            ArtworkListingId = order.ArtworkListingId,
            ArtworkTitle = order.ArtworkListing?.Title ?? string.Empty,
            ArtworkImageUrl = order.ArtworkListing?.ImageUrl,
            ArtworkCategory = order.ArtworkListing?.Category ?? ArtworkCategory.Other,
            Price = order.Price,
            BuyerId = order.BuyerId,
            BuyerDisplayName = order.Buyer?.DisplayName ?? string.Empty,
            BuyerCity = order.Buyer?.City ?? string.Empty,
            SellerId = order.SellerId,
            SellerDisplayName = order.Seller?.DisplayName ?? string.Empty,
            SellerCity = order.Seller?.City ?? string.Empty,
            Status = order.Status,
            PickupNotes = order.PickupNotes,
            CancellationReason = order.CancellationReason,
            CreatedAtUtc = order.CreatedAtUtc,
            HandedOverAtUtc = order.HandedOverAtUtc,
            CompletedAtUtc = order.CompletedAtUtc,
            CancelledAtUtc = order.CancelledAtUtc
        };
    }
}
