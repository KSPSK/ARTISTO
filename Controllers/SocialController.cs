using ARTISTO.Data;
using ARTISTO.Extensions;
using ARTISTO.Models;
using ARTISTO.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ARTISTO.Controllers;

[ApiController]
[Route("api")]
public class SocialController(ISocialService social) : ControllerBase
{
    private int? Viewer => User.Identity?.IsAuthenticated == true && User.TryGetUserId(out var id) ? id : null;

    private IActionResult ToResponse<T>(ServiceResult<T> result) => result.Status switch
    {
        ServiceStatus.Success => Ok(result.Value),
        ServiceStatus.NotFound => NotFound(new ApiErrorResponse(result.ErrorMessage!)),
        ServiceStatus.Unauthorized => Unauthorized(new ApiErrorResponse(result.ErrorMessage!)),
        ServiceStatus.BadRequest => BadRequest(new ApiErrorResponse(result.ErrorMessage!)),
        _ => StatusCode(StatusCodes.Status500InternalServerError)
    };

    private async Task<IActionResult> Set(SocialAction action, int target, bool added)
    {
        if (Viewer is not int actor)
        {
            return Unauthorized(new ApiErrorResponse("The authentication session is invalid."));
        }
        var result = await social.SetAsync(action, actor, target, added);
        return result.Status switch
        {
            ServiceStatus.Success => NoContent(),
            ServiceStatus.NotFound => NotFound(new ApiErrorResponse(result.ErrorMessage!)),
            ServiceStatus.Unauthorized => Unauthorized(new ApiErrorResponse(result.ErrorMessage!)),
            ServiceStatus.BadRequest => BadRequest(new ApiErrorResponse(result.ErrorMessage!)),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [Authorize, HttpPut("artworklistings/{id:int}/like")]
    public Task<IActionResult> Like(int id) => Set(SocialAction.Like, id, true);

    [Authorize, HttpDelete("artworklistings/{id:int}/like")]
    public Task<IActionResult> Unlike(int id) => Set(SocialAction.Like, id, false);

    [HttpGet("artworklistings/{id:int}/likes")]
    public async Task<IActionResult> Likes(int id) => ToResponse(await social.LikesAsync(id, Viewer));

    [Authorize, HttpPut("artworklistings/{id:int}/favorite")]
    public Task<IActionResult> Save(int id) => Set(SocialAction.Favorite, id, true);

    [Authorize, HttpDelete("artworklistings/{id:int}/favorite")]
    public Task<IActionResult> Unsave(int id) => Set(SocialAction.Favorite, id, false);

    [Authorize, HttpGet("artworklistings/{id:int}/favorite")]
    public async Task<IActionResult> Favorite(int id)
    {
        return Viewer is int actor ? ToResponse(await social.FavoriteAsync(id, actor))
            : Unauthorized(new ApiErrorResponse("The authentication session is invalid."));
    }

    [Authorize, HttpGet("users/me/favorites")]
    public async Task<IActionResult> Favorites([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        return Viewer is int actor ? ToResponse(await social.FavoritesAsync(actor, page, pageSize))
            : Unauthorized(new ApiErrorResponse("The authentication session is invalid."));
    }

}
