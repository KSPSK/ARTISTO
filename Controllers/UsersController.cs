using ARTISTO.Models;
using ARTISTO.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ARTISTO.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("register")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserResponse>> Register(UserRegisterRequest request)
    {
        var result = await _userService.RegisterAsync(request);
        if (!result.IsSuccess)
        {
            return BadRequest(new ApiErrorResponse(result.ErrorMessage!));
        }

        var user = result.Value!;
        await SignInUser(user);

        return CreatedAtAction(
            nameof(GetById),
            new { id = user.Id },
            UserService.ToResponse(user));
    }

    [HttpPost("login")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> Login(UserLoginRequest request)
    {
        var result = await _userService.AuthenticateAsync(request);
        if (!result.IsSuccess)
        {
            return Unauthorized(new ApiErrorResponse(result.ErrorMessage!));
        }

        var user = result.Value!;
        await SignInUser(user);

        return Ok(UserService.ToResponse(user));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> GetCurrentUser()
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new ApiErrorResponse("The authentication session is invalid."));
        }

        var user = await _userService.GetByIdAsync(userId);
        if (user is null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Unauthorized(new ApiErrorResponse("The authenticated user no longer exists."));
        }

        return Ok(user);
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetAll()
    {
        var users = await _userService.GetAllAsync();
        return Ok(users);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetById(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user is null)
        {
            return NotFound(new ApiErrorResponse($"User {id} was not found."));
        }

        return Ok(user);
    }

    [HttpGet("{id:int}/artworklistings")]
    [ProducesResponseType<IEnumerable<ArtworkListing>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ArtworkListing>>> GetListings(int id)
    {
        var result = await _userService.GetListingsAsync(id);
        if (!result.IsSuccess)
        {
            return NotFound(new ApiErrorResponse(result.ErrorMessage!));
        }

        return Ok(result.Value);
    }

    [Authorize]
    [HttpPut("{id:int}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> Update(int id, UserUpdateRequest request)
    {
        if (!TryGetAuthenticatedUserId(out var currentUserId))
        {
            return Unauthorized(new ApiErrorResponse("The authentication session is invalid."));
        }

        var result = await _userService.UpdateAsync(id, request, currentUserId);

        return result.Status switch
        {
            ServiceStatus.Success => Ok(result.Value),
            ServiceStatus.NotFound => NotFound(new ApiErrorResponse(result.ErrorMessage!)),
            ServiceStatus.Forbidden => Forbid(),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    private async Task SignInUser(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = false });
    }

    private bool TryGetAuthenticatedUserId(out int userId)
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
}
