using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ARTISTO.Models;

public class Review
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    [JsonIgnore]
    public Order? Order { get; set; }

    public int ArtworkListingId { get; set; }

    [JsonIgnore]
    public ArtworkListing? ArtworkListing { get; set; }

    public int ReviewerId { get; set; }

    [JsonIgnore]
    public User? Reviewer { get; set; }

    public int SellerId { get; set; }

    [JsonIgnore]
    public User? Seller { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}