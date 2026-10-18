using System.ComponentModel.DataAnnotations;

namespace ARTISTO.Models;

public sealed class CreateOrderRequest
{
    [Required]
    public int ArtworkListingId { get; set; }

    [MaxLength(1000)]
    public string? PickupNotes { get; set; }
}

public sealed class CancelOrderRequest
{
    [MaxLength(500)]
    public string? Reason { get; set; }
}

public sealed class OrderResponse
{
    public int Id { get; set; }

    public int ArtworkListingId { get; set; }

    public string ArtworkTitle { get; set; } = string.Empty;

    public string? ArtworkImageUrl { get; set; }

    public ArtworkCategory ArtworkCategory { get; set; }

    public decimal Price { get; set; }

    public int BuyerId { get; set; }

    public string BuyerDisplayName { get; set; } = string.Empty;

    public string BuyerCity { get; set; } = string.Empty;

    public int SellerId { get; set; }

    public string SellerDisplayName { get; set; } = string.Empty;

    public string SellerCity { get; set; } = string.Empty;

    public OrderStatus Status { get; set; }

    public string? PickupNotes { get; set; }

    public string? CancellationReason { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? HandedOverAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public DateTime? CancelledAtUtc { get; set; }
}
