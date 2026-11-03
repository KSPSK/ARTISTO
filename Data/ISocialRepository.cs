using ARTISTO.Models;

namespace ARTISTO.Data;

public enum SocialAction { Like, Favorite }

public interface ISocialRepository
{
    Task<bool> UserExistsAsync(int id);
    Task<bool> ArtworkExistsAsync(int id);
    Task SetAsync(SocialAction action, int actorId, int targetId, bool added);
    Task<LikeResponse?> LikesAsync(int id, int? viewer);
    Task<FavoriteResponse?> FavoriteAsync(int id, int actor);
    Task<PageResponse<ArtworkResponse>> FavoritesAsync(int actor, int page, int size);
    Task<List<ArtworkResponse>> ArtworksAsync(IEnumerable<ArtworkListing> artworks, int? viewer);
}
