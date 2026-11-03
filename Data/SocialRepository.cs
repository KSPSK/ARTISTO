using ARTISTO.Models;
using Microsoft.EntityFrameworkCore;

namespace ARTISTO.Data;

public class SocialRepository(ArtistoDbContext db) : ISocialRepository
{
    public Task<bool> UserExistsAsync(int id) => db.Users.AnyAsync(x => x.Id == id);
    public Task<bool> ArtworkExistsAsync(int id) => db.ArtworkListings.AnyAsync(x => x.Id == id);

    public async Task SetAsync(SocialAction action, int actorId, int targetId, bool added)
    {
        // Explicit conflict targets ignore only this pair's duplicate key. FK/check and other errors propagate.
        if (added)
        {
            switch (action)
            {
                case SocialAction.Like:
                    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"ArtworkLikes\" (\"UserId\", \"ArtworkListingId\") VALUES ({actorId}, {targetId}) ON CONFLICT (\"UserId\", \"ArtworkListingId\") DO NOTHING");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }
        }
        else
        {
            switch (action)
            {
                case SocialAction.Like:
                    await db.ArtworkLikes.Where(x => x.UserId == actorId && x.ArtworkListingId == targetId).ExecuteDeleteAsync();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action));
            }
        }
    }

    public Task<LikeResponse?> LikesAsync(int id, int? viewer) => db.ArtworkListings.Where(x => x.Id == id)
        .Select(x => new LikeResponse(x.Id, db.ArtworkLikes.Count(l => l.ArtworkListingId == x.Id),
            viewer != null && db.ArtworkLikes.Any(l => l.ArtworkListingId == x.Id && l.UserId == viewer)))
        .SingleOrDefaultAsync();

    private IQueryable<ArtworkResponse> ProjectArtworks(IQueryable<ArtworkListing> query, int? viewer) => query.Select(x => new ArtworkResponse
    {
        Id = x.Id,
        Title = x.Title,
        Description = x.Description,
        Price = x.Price,
        CreatorName = x.CreatorName,
        Category = x.Category,
        Status = x.Status,
        LocalPickupAvailable = x.LocalPickupAvailable,
        ImageUrl = x.ImageUrl,
        UserId = x.UserId,
        IsAvailable = x.Status == ArtworkListingStatus.Available,
        HasImage = x.ImageUrl != null && x.ImageUrl.Trim() != "",
        LikeCount = db.ArtworkLikes.Count(l => l.ArtworkListingId == x.Id),
        IsLiked = viewer != null && db.ArtworkLikes.Any(l => l.ArtworkListingId == x.Id && l.UserId == viewer)
    });

    public async Task<List<ArtworkResponse>> ArtworksAsync(IEnumerable<ArtworkListing> artworks, int? viewer)
    {
        var ids = artworks.Select(x => x.Id).ToArray();
        var responses = await ProjectArtworks(db.ArtworkListings.Where(x => ids.Contains(x.Id)), viewer).ToDictionaryAsync(x => x.Id);
        return ids.Where(responses.ContainsKey).Select(id => responses[id]).ToList();
    }

}
