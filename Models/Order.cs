using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ARTISTO.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderStatus
{
    Pending,
    HandedOver,
    Completed,
    Cancelled
}

public class Order
{
    public int Id { get; set; }

    public int ArtworkListingId { get; set; }

    public ArtworkListing? ArtworkListing { get; set; }

    public int BuyerId { get; set; }

    public User? Buyer { get; set; }

    public int SellerId { get; set; }

    public User? Seller { get; set; }

    [Range(0.01, 1000000)]
    public decimal Price { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [MaxLength(1000)]
    public string? PickupNotes { get; set; }

    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? HandedOverAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public DateTime? CancelledAtUtc { get; set; }
}
