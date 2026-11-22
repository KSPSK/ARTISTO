using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ARTISTO.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OfferStatus
{
    Pending = 0,
    Accepted = 1,
    Declined = 2,
    Countered = 3
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

    [ConcurrencyCheck]
    public OfferStatus Status { get; set; } = OfferStatus.Pending;

    [Range(typeof(decimal), "0.01", "1000000")]
    public decimal? CounterAmount { get; set; }

    [MaxLength(1000)]
    public string? CounterMessage { get; set; }

    public int? OrderId { get; set; }

    [JsonIgnore]
    public Order? Order { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    
    public DateTime? RespondedAtUtc { get; set; }
}
