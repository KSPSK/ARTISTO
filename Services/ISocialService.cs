using ARTISTO.Data;
using ARTISTO.Models;

namespace ARTISTO.Services;

public interface ISocialService
{
    Task<ServiceResult> SetAsync(SocialAction action, int actor, int target, bool added);
    Task<ServiceResult<LikeResponse>> LikesAsync(int id, int? viewer);
    Task<List<ArtworkResponse>> ArtworksAsync(IEnumerable<ArtworkListing> artworks, int? viewer);
}
