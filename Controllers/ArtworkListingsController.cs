using ARTISTO.Models;
using ARTISTO.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ARTISTO.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArtworkListingsController : ControllerBase
{
    private readonly ArtistoDbContext _context;

    public ArtworkListingsController(ArtistoDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ArtworkListing>>> GetAll([FromQuery] string? q)
    {
        var query = _context.ArtworkListings.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var search = q.Trim();
            query = query.Where(item =>
                (item.Title != null && EF.Functions.ILike(item.Title, $"%{search}%")) ||
                (item.CreatorName != null && EF.Functions.ILike(item.CreatorName, $"%{search}%")));
        }
        var results = await query.ToListAsync();
        return Ok(results);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ArtworkListing>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArtworkListing>> GetById(int id)
    {
        var listing = await _context.ArtworkListings.FindAsync(id);
        if (listing is null)
        {
            return NotFound(CreateNotFoundResponse(id));
        }
        return Ok(listing);
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType<ArtworkListing>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ArtworkListing>> Create(ArtworkListingWriteRequest request)
    {
        if (!TryGetAuthenticatedUserId(out var userId) ||
            !await _context.Users.AnyAsync(u => u.Id == userId))
        {
            return Unauthorized(new ApiErrorResponse(
                "A valid user account is required to create a listing."));
        }

        var listing = new ArtworkListing
        {
            Title = request.Title,
            Description = request.Description,
            Price = request.Price,
            CreatorName = request.CreatorName,
            Category = request.Category,
            LocalPickupAvailable = request.LocalPickupAvailable,
            UserId = userId
        };

        _context.ArtworkListings.Add(listing);
        await _context.SaveChangesAsync();
        return CreatedAtAction(
            nameof(GetById),
            new { id = listing.Id },
            listing);
    }

    [Authorize]
    [HttpPut("{id:int}")]
    [ProducesResponseType<ArtworkListing>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArtworkListing>> Update(
        int id,
        ArtworkListingWriteRequest request)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new ApiErrorResponse("The authentication session is invalid."));
        }

        var existing = await _context.ArtworkListings.FindAsync(id);
        if (existing is null)
        {
            return NotFound(CreateNotFoundResponse(id));
        }

        if (existing.UserId is null || existing.UserId != userId)
        {
            return Forbid();
        }

        existing.Title = request.Title;
        existing.Description = request.Description;
        existing.Price = request.Price;
        existing.CreatorName = request.CreatorName;
        existing.Category = request.Category;
        existing.LocalPickupAvailable = request.LocalPickupAvailable;

        await _context.SaveChangesAsync();
        return Ok(existing);
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        if (!TryGetAuthenticatedUserId(out var userId))
        {
            return Unauthorized(new ApiErrorResponse("The authentication session is invalid."));
        }

        var existing = await _context.ArtworkListings.FindAsync(id);
        if (existing is null)
        {
            return NotFound(CreateNotFoundResponse(id));
        }

        if (existing.UserId is null || existing.UserId != userId)
        {
            return Forbid();
        }

        _context.ArtworkListings.Remove(existing);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static ApiErrorResponse CreateNotFoundResponse(int id)
    {
        return new ApiErrorResponse($"Artwork listing {id} was not found.");
    }

    private bool TryGetAuthenticatedUserId(out int userId)
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
    }
}
