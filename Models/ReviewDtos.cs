using System.ComponentModel.DataAnnotations;

namespace ARTISTO.Models;

public sealed record CreateReviewRequest
{
    [Range(1, int.MaxValue)]
    public int OrderId { get; init; }

    [Range(1, 5)]
    public int Rating { get; init; }

    [MaxLength(1000)]
    public string? Comment { get; init; }
}

public sealed record ReviewResponse
{
    public int Id { get; init; }

    public int OrderId { get; init; }

    public int ArtworkListingId { get; init; }

    public string ArtworkTitle { get; init; } = string.Empty;

    public int ReviewerId { get; init; }

    public string ReviewerDisplayName { get; init; } = string.Empty;

    public int SellerId { get; init; }

    public int Rating { get; init; }

    public string? Comment { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}

public sealed record SellerReviewsResponse
{
    public int SellerId { get; init; }

    public double? AverageRating { get; init; }

    public int ReviewCount { get; init; }

    public IReadOnlyList<ReviewResponse> Reviews { get; init; } = [];
}