using ARTISTO.Data;
using ARTISTO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ARTISTO.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ArtistoDbContext _context;

    public OrdersController(ArtistoDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType<IEnumerable<OrderResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetMyOrders([FromQuery] string? role)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new ApiErrorResponse(
                "The authentication session is invalid."));
        }

        role = role?.Trim();

        if (!string.IsNullOrEmpty(role)
            && !string.Equals(role, "buyer", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(role, "seller", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ApiErrorResponse(
                "Role must be either 'buyer' or 'seller'."));
        }

        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.ArtworkListing)
            .Include(o => o.Buyer)
            .Include(o => o.Seller)
            .AsQueryable();

        if (string.Equals(role, "buyer", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(o => o.BuyerId == userId);
        }
        else if (string.Equals(role, "seller", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(o => o.SellerId == userId);
        }
        else
        {
            query = query.Where(o => o.BuyerId == userId || o.SellerId == userId);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .ThenByDescending(o => o.Id)
            .ToListAsync();

        return Ok(orders.Select(ToResponse));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetById(int id)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new ApiErrorResponse(
                "The authentication session is invalid."));
        }

        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.ArtworkListing)
            .Include(o => o.Buyer)
            .Include(o => o.Seller)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound(CreateNotFoundResponse(id));
        }

        if (order.BuyerId != userId && order.SellerId != userId)
        {
            return Forbid();
        }

        return Ok(ToResponse(order));
    }

    [HttpPost]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request)
    {
        if (!TryGetAuthenticatedUserId(out var buyerId) ||
            !await _context.Users.AnyAsync(u => u.Id == buyerId))
        {
            return Unauthorized(new ApiErrorResponse("A valid user account is required to place an order."));
        }

        var listing = await _context.ArtworkListings
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == request.ArtworkListingId);

        if (listing is null)
        {
            return NotFound(new ApiErrorResponse($"Artwork listing {request.ArtworkListingId} was not found."));
        }

        if (listing.UserId is null || listing.User is null)
        {
            return BadRequest(new ApiErrorResponse(
                "This artwork listing has no seller and cannot be ordered."));
        }

        if (listing.UserId == buyerId)
        {
            return BadRequest(new ApiErrorResponse(
                "You cannot order your own artwork listing."));
        }

        if (!listing.LocalPickupAvailable)
        {
            return BadRequest(new ApiErrorResponse(
                "This artwork listing does not offer local pickup and cannot be ordered."));
        }

        if (listing.Status != ArtworkListingStatus.Available)
        {
            return Conflict(new ApiErrorResponse(
                $"This artwork listing is currently {listing.Status.ToString().ToLowerInvariant()} and cannot be ordered."));
        }

        var buyer = await _context.Users.FindAsync(buyerId);
        if (buyer is null)
        {
            return Unauthorized(new ApiErrorResponse("Buyer account not found."));
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

        _context.Orders.Add(order);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new ApiErrorResponse(
                "This artwork listing was already reserved by another concurrent order."));
        }

        var response = ToResponse(order);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, response);
    }

    [HttpPost("{id:int}/handover")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> ConfirmHandover(int id)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new ApiErrorResponse("The authentication session is invalid."));
        }

        var order = await _context.Orders
            .Include(o => o.ArtworkListing)
            .Include(o => o.Buyer)
            .Include(o => o.Seller)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound(CreateNotFoundResponse(id));
        }

        if (order.SellerId != userId)
        {
            return Forbid();
        }

        if (order.Status != OrderStatus.Pending)
        {
            return BadRequest(new ApiErrorResponse(
                $"Handover can only be confirmed for pending orders. Current status: {order.Status}."));
        }

        order.Status = OrderStatus.HandedOver;
        order.HandedOverAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(ToResponse(order));
    }

    [HttpPost("{id:int}/complete")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> ConfirmReceipt(int id)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new ApiErrorResponse("The authentication session is invalid."));
        }

        var order = await _context.Orders
            .Include(o => o.ArtworkListing)
            .Include(o => o.Buyer)
            .Include(o => o.Seller)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound(CreateNotFoundResponse(id));
        }

        if (order.BuyerId != userId)
        {
            return Forbid();
        }

        if (order.Status != OrderStatus.HandedOver)
        {
            return BadRequest(new ApiErrorResponse(
                $"Order receipt can only be confirmed after the seller has handed it over. Current status: {order.Status}."));
        }

        order.Status = OrderStatus.Completed;
        order.CompletedAtUtc = DateTime.UtcNow;

        if (order.ArtworkListing is not null)
        {
            order.ArtworkListing.Status = ArtworkListingStatus.Sold;
        }

        await _context.SaveChangesAsync();
        return Ok(ToResponse(order));
    }

    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> Cancel(int id, CancelOrderRequest? request)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new ApiErrorResponse("The authentication session is invalid."));
        }

        var order = await _context.Orders
            .Include(o => o.ArtworkListing)
            .Include(o => o.Buyer)
            .Include(o => o.Seller)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            return NotFound(CreateNotFoundResponse(id));
        }

        if (order.BuyerId != userId && order.SellerId != userId)
        {
            return Forbid();
        }

        if (order.Status != OrderStatus.Pending)
        {
            return BadRequest(new ApiErrorResponse(
                $"Only pending orders can be cancelled. Current status is '{order.Status}'."));
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

        await _context.SaveChangesAsync();
        return Ok(ToResponse(order));
    }

    private static OrderResponse ToResponse(Order order)
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

    private static ApiErrorResponse CreateNotFoundResponse(int id)
    {
        return new ApiErrorResponse($"Order {id} was not found.");
    }

    private bool TryGetAuthenticatedUserId(out int userId)
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
}
