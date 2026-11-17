using System.ComponentModel.DataAnnotations;

namespace ARTISTO.Models;

public sealed record CreateOfferRequest
{
    [Range(1, int.MaxValue)]
    public int ArtworkListingId { get; init; }

    [Range(typeof(decimal), "0.01", "1000000", ParseLimitsInInvariantCulture = true)]
    public decimal Amount { get; init; }

    [MaxLength(1000)]
    public string? Message { get; init; }
}

public sealed record CounterOfferRequest
{
    [Range(typeof(decimal), "0.01", "1000000", ParseLimitsInInvariantCulture = true)]
    public decimal Amount { get; init; }

    [MaxLength(1000)]
    public string? Message { get; init; }
}

public sealed record OfferResponse
{
    public int Id { get; init; }
    public int ArtworkListingId { get; init; }
    public string ArtworkTitle { get; init; } = string.Empty;
    public string? ArtworkImageUrl { get; init; }
    public decimal ListingPrice { get; init; }
    public int BuyerId { get; init; }
    public string BuyerDisplayName { get; init; } = string.Empty;
    public int? SellerId { get; init; }
    public string SellerDisplayName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string? Message { get; init; }
    public OfferStatus Status { get; init; }
    public decimal? CounterAmount { get; init; }
    public string? CounterMessage { get; init; }
    public int? OrderId { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? RespondedAtUtc { get; init; }
}
