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
                case SocialAction.Favorite:
                    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"ArtworkFavorites\" (\"UserId\", \"ArtworkListingId\") VALUES ({actorId}, {targetId}) ON CONFLICT (\"UserId\", \"ArtworkListingId\") DO NOTHING");
                    break;
                case SocialAction.Follow:
                    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"UserFollows\" (\"FollowerId\", \"FollowedUserId\") VALUES ({actorId}, {targetId}) ON CONFLICT (\"FollowerId\", \"FollowedUserId\") DO NOTHING");
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
                case SocialAction.Favorite:
                    await db.ArtworkFavorites.Where(x => x.UserId == actorId && x.ArtworkListingId == targetId).ExecuteDeleteAsync();
                    break;
                case SocialAction.Follow:
                    await db.UserFollows.Where(x => x.FollowerId == actorId && x.FollowedUserId == targetId).ExecuteDeleteAsync();
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

    public Task<FavoriteResponse?> FavoriteAsync(int id, int actor) => db.ArtworkListings.Where(x => x.Id == id)
        .Select(x => new FavoriteResponse(x.Id, db.ArtworkFavorites.Any(f => f.ArtworkListingId == x.Id && f.UserId == actor)))
        .SingleOrDefaultAsync();

    public Task<FollowResponse?> FollowsAsync(int id, int? viewer) => db.Users.Where(x => x.Id == id)
        .Select(x => new FollowResponse(x.Id, db.UserFollows.Count(f => f.FollowedUserId == x.Id),
            db.UserFollows.Count(f => f.FollowerId == x.Id),
            viewer != null && db.UserFollows.Any(f => f.FollowerId == viewer && f.FollowedUserId == x.Id)))
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
        IsLiked = viewer != null && db.ArtworkLikes.Any(l => l.ArtworkListingId == x.Id && l.UserId == viewer),
        IsSaved = viewer != null && db.ArtworkFavorites.Any(f => f.ArtworkListingId == x.Id && f.UserId == viewer)
    });

    private IQueryable<UserResponse> ProjectUsers(IQueryable<User> query, int? viewer) => query.Select(x => new UserResponse
    {
        Id = x.Id,
        Username = x.Username,
        DisplayName = x.DisplayName,
        Description = x.Description,
        City = x.City,
        FollowerCount = db.UserFollows.Count(f => f.FollowedUserId == x.Id),
        FollowingCount = db.UserFollows.Count(f => f.FollowerId == x.Id),
        IsFollowing = viewer != null && db.UserFollows.Any(f => f.FollowerId == viewer && f.FollowedUserId == x.Id)
    });

    public async Task<PageResponse<ArtworkResponse>> FavoritesAsync(int actor, int page, int size)
    {
        var query = db.ArtworkListings.Where(x => db.ArtworkFavorites.Any(f => f.UserId == actor && f.ArtworkListingId == x.Id));
        var count = await query.CountAsync();
        var items = await ProjectArtworks(query.OrderBy(x => x.Id).Skip((page - 1) * size).Take(size), actor).ToListAsync();
        return new(items, page, size, count);
    }

    public async Task<PageResponse<UserResponse>> ConnectionsAsync(int id, bool followers, int? viewer, int page, int size)
    {
        var query = db.Users.Where(x => followers
            ? db.UserFollows.Any(f => f.FollowedUserId == id && f.FollowerId == x.Id)
            : db.UserFollows.Any(f => f.FollowerId == id && f.FollowedUserId == x.Id));
        var count = await query.CountAsync();
        var items = await ProjectUsers(query.OrderBy(x => x.Id).Skip((page - 1) * size).Take(size), viewer).ToListAsync();
        return new(items, page, size, count);
    }

    public async Task<List<ArtworkResponse>> ArtworksAsync(IEnumerable<ArtworkListing> artworks, int? viewer)
    {
        var ids = artworks.Select(x => x.Id).ToArray();
        var responses = await ProjectArtworks(db.ArtworkListings.Where(x => ids.Contains(x.Id)), viewer).ToDictionaryAsync(x => x.Id);
        return ids.Where(responses.ContainsKey).Select(id => responses[id]).ToList();
    }

    public async Task<List<UserResponse>> UsersAsync(IEnumerable<UserResponse> users, int? viewer)
    {
        var ids = users.Select(x => x.Id).ToArray();
        var responses = await ProjectUsers(db.Users.Where(x => ids.Contains(x.Id)), viewer).ToDictionaryAsync(x => x.Id);
        return ids.Where(responses.ContainsKey).Select(id => responses[id]).ToList();
    }
}
