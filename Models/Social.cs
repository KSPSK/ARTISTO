namespace ARTISTO.Models;

public class ArtworkLike
{
    public int UserId { get; set; }
    public int ArtworkListingId { get; set; }
}

public class ArtworkFavorite
{
    public int UserId { get; set; }
    public int ArtworkListingId { get; set; }
}

public sealed record LikeResponse(int ArtworkListingId, int LikeCount, bool IsLiked);
public sealed record FavoriteResponse(int ArtworkListingId, bool IsSaved);
public sealed record PageResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed record ArtworkResponse
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string CreatorName { get; init; } = string.Empty;
    public ArtworkCategory Category { get; init; }
    public ArtworkListingStatus Status { get; init; }
    public bool LocalPickupAvailable { get; init; }
    public string? ImageUrl { get; init; }
    public int? UserId { get; init; }
    public bool IsAvailable { get; init; }
    public bool HasImage { get; init; }
    public int LikeCount { get; init; }
    public bool IsLiked { get; init; }
    public bool IsSaved { get; init; }
}
