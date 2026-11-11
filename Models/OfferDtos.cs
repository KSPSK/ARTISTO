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

public sealed record OfferResponse(
    int Id,
    int ArtworkListingId,
    int BuyerId,
    decimal Amount,
    string? Message,
    OfferStatus Status,
    DateTime CreatedAtUtc);
