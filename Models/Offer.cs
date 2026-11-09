using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ARTISTO.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OfferStatus
{
    Pending
}

public class Offer
{
    public int Id { get; set; }

    public int ArtworkListingId { get; set; }

    [JsonIgnore]
    public ArtworkListing? ArtworkListing { get; set; }

    public int BuyerId { get; set; }

    [JsonIgnore]
    public User? Buyer { get; set; }

    [Range(typeof(decimal), "0.01", "1000000")]
    public decimal Amount { get; set; }

    [MaxLength(1000)]
    public string? Message { get; set; }

    public OfferStatus Status { get; set; } = OfferStatus.Pending;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
