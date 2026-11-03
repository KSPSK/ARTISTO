using ARTISTO.Data;
using ARTISTO.Models;

namespace ARTISTO.Services;

public class SocialService(ISocialRepository repository) : ISocialService
{
    public async Task<ServiceResult> SetAsync(SocialAction action, int actor, int target, bool added)
    {
        if (!await repository.UserExistsAsync(actor))
        {
            return ServiceResult.Unauthorized("The authenticated user no longer exists.");
        }
        var exists = await repository.ArtworkExistsAsync(target);
        if (!exists)
        {
            return ServiceResult.NotFound($"Artwork listing {target} was not found.");
        }
        await repository.SetAsync(action, actor, target, added);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<LikeResponse>> LikesAsync(int id, int? viewer)
    {
        var value = await repository.LikesAsync(id, viewer);
        return value is null ? ServiceResult<LikeResponse>.NotFound($"Artwork listing {id} was not found.") : ServiceResult<LikeResponse>.Success(value);
    }

    public Task<List<ArtworkResponse>> ArtworksAsync(IEnumerable<ArtworkListing> artworks, int? viewer) => repository.ArtworksAsync(artworks, viewer);
}
