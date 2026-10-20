using System.ComponentModel.DataAnnotations;

namespace ARTISTO.Models;

public sealed record CreateOrderRequest
{
    [Required]
    public int ArtworkListingId { get; init; }

    [MaxLength(1000)]
    public string? PickupNotes { get; init; }
}

public sealed record CancelOrderRequest
{
    [MaxLength(500)]
    public string? Reason { get; init; }
}

public sealed record OrderResponse
{
    public int Id { get; init; }

    public int ArtworkListingId { get; init; }

    public string ArtworkTitle { get; init; } = string.Empty;

    public string? ArtworkImageUrl { get; init; }

    public ArtworkCategory ArtworkCategory { get; init; }

    public decimal Price { get; init; }

    public int BuyerId { get; init; }

    public string BuyerDisplayName { get; init; } = string.Empty;

    public string BuyerCity { get; init; } = string.Empty;

    public int SellerId { get; init; }

    public string SellerDisplayName { get; init; } = string.Empty;

    public string SellerCity { get; init; } = string.Empty;

    public OrderStatus Status { get; init; }

    public string? PickupNotes { get; init; }

    public string? CancellationReason { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? HandedOverAtUtc { get; init; }

    public DateTime? CompletedAtUtc { get; init; }

    public DateTime? CancelledAtUtc { get; init; }
}
