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

    public async Task<ServiceResult<FavoriteResponse>> FavoriteAsync(int id, int actor)
    {
        if (!await repository.UserExistsAsync(actor))
        {
            return ServiceResult<FavoriteResponse>.Unauthorized("The authenticated user no longer exists.");
        }
        var value = await repository.FavoriteAsync(id, actor);
        return value is null ? ServiceResult<FavoriteResponse>.NotFound($"Artwork listing {id} was not found.") : ServiceResult<FavoriteResponse>.Success(value);
    }

    private static bool ValidPage(int page, int size) => page >= 1 && size >= 1 && size <= 100 && (long)(page - 1) * size <= int.MaxValue;

    public async Task<ServiceResult<PageResponse<ArtworkResponse>>> FavoritesAsync(int actor, int page, int size)
    {
        if (!await repository.UserExistsAsync(actor))
        {
            return ServiceResult<PageResponse<ArtworkResponse>>.Unauthorized("The authenticated user no longer exists.");
        }
        if (!ValidPage(page, size))
        {
            return ServiceResult<PageResponse<ArtworkResponse>>.BadRequest("Page must be positive and pageSize must be between 1 and 100.");
        }
        return ServiceResult<PageResponse<ArtworkResponse>>.Success(await repository.FavoritesAsync(actor, page, size));
    }

    public Task<List<ArtworkResponse>> ArtworksAsync(IEnumerable<ArtworkListing> artworks, int? viewer) => repository.ArtworksAsync(artworks, viewer);
}
