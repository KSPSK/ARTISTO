using ARTISTO.Models;

namespace ARTISTO.Data;

public enum SocialAction { Like }

public interface ISocialRepository
{
    Task<bool> UserExistsAsync(int id);
    Task<bool> ArtworkExistsAsync(int id);
    Task SetAsync(SocialAction action, int actorId, int targetId, bool added);
    Task<LikeResponse?> LikesAsync(int id, int? viewer);
    Task<List<ArtworkResponse>> ArtworksAsync(IEnumerable<ArtworkListing> artworks, int? viewer);
}
