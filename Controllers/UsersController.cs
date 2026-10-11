using ARTISTO.Data;
using ARTISTO.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ARTISTO.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ArtistoDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UsersController(
        ArtistoDbContext context,
        IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    [HttpPost("register")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserResponse>> Register(UserRegisterRequest request)
    {
        var normalizedUsername = request.Username.Trim().ToLowerInvariant();

        var usernameExists = await _context.Users.AnyAsync(u => u.Username.ToLower() == normalizedUsername);
        if (usernameExists)
        {
            return BadRequest(new ApiErrorResponse($"Username '{request.Username}' is already taken."));
        }

        var user = new User
        {
            Username = request.Username.Trim(),
            DisplayName = request.DisplayName.Trim(),
            Description = request.Description.Trim(),
            City = request.City.Trim()
        };

        user.Password = _passwordHasher.HashPassword(user, request.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        await SignInUser(user);

        return CreatedAtAction(
            nameof(GetById),
            new { id = user.Id },
            ToResponse(user));
    }

    [HttpPost("login")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> Login(UserLoginRequest request)
    {
        var normalizedUsername = request.Username.Trim().ToLowerInvariant();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == normalizedUsername);
        if (user is null)
        {
            return Unauthorized(new ApiErrorResponse("Invalid username or password."));
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.Password, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new ApiErrorResponse("Invalid username or password."));
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.Password = _passwordHasher.HashPassword(user, request.Password);
            await _context.SaveChangesAsync();
        }

        await SignInUser(user);

        return Ok(ToResponse(user));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> GetCurrentUser()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return Unauthorized(new ApiErrorResponse("The authentication session is invalid."));
        }

        var user = await _context.Users.FindAsync(userId);
        if (user is null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Unauthorized(new ApiErrorResponse("The authenticated user no longer exists."));
        }

        return Ok(ToResponse(user));
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
        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.DisplayName)
            .ToListAsync();

        return Ok(users.Select(ToResponse));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetById(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null)
        {
            return NotFound(CreateNotFoundResponse(id));
        }
        return Ok(ToResponse(user));
    }

    [HttpGet("{id:int}/artworklistings")]
    [ProducesResponseType<IEnumerable<ArtworkListing>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ArtworkListing>>> GetListings(int id)
    {
        var exists = await _context.Users.AnyAsync(u => u.Id == id);
        if (!exists)
        {
            return NotFound(CreateNotFoundResponse(id));
        }

        var listings = await _context.ArtworkListings
            .AsNoTracking()
            .Where(a => a.UserId == id)
            .ToListAsync();

        return Ok(listings);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> Update(int id, UserUpdateRequest request)
    {
        var existing = await _context.Users.FindAsync(id);
        if (existing is null)
        {
            return NotFound(CreateNotFoundResponse(id));
        }

        existing.DisplayName = request.DisplayName.Trim();
        existing.Description = request.Description.Trim();
        existing.City = request.City.Trim();
        await _context.SaveChangesAsync();

        return Ok(ToResponse(existing));
    }

    private static UserResponse ToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Description = user.Description,
            City = user.City
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

    private static ApiErrorResponse CreateNotFoundResponse(int id)
    {
        return new ApiErrorResponse($"User {id} was not found.");
    }
}
