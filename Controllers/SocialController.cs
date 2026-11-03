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

}
