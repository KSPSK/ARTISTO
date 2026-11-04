using ARTISTO.Models;

namespace ARTISTO.Data;

public enum SocialAction { Like, Favorite, Follow }

public interface ISocialRepository
{
    Task<bool> UserExistsAsync(int id);
    Task<bool> ArtworkExistsAsync(int id);
    Task SetAsync(SocialAction action, int actorId, int targetId, bool added);
    Task<LikeResponse?> LikesAsync(int id, int? viewer);
    Task<FavoriteResponse?> FavoriteAsync(int id, int actor);
    Task<FollowResponse?> FollowsAsync(int id, int? viewer);
    Task<PageResponse<ArtworkResponse>> FavoritesAsync(int actor, int page, int size);
    Task<PageResponse<UserResponse>> ConnectionsAsync(int id, bool followers, int? viewer, int page, int size);
    Task<List<ArtworkResponse>> ArtworksAsync(IEnumerable<ArtworkListing> artworks, int? viewer);
    Task<List<UserResponse>> UsersAsync(IEnumerable<UserResponse> users, int? viewer);
}
