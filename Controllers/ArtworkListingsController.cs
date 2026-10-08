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
    public async Task<ActionResult<ArtworkListing>> Create(ArtworkListing listing)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId) ||
            !await _context.Users.AnyAsync(u => u.Id == userId))
        {
            return Unauthorized(new ApiErrorResponse(
                "A valid user account is required to create a listing."));
        }

        listing.UserId = userId;
        _context.ArtworkListings.Add(listing);
        await _context.SaveChangesAsync();
        return CreatedAtAction(
            nameof(GetById),
            new { id = listing.Id },
            listing);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ArtworkListing>> Update(int id, ArtworkListing listing)
    {
        var existing = await _context.ArtworkListings.FindAsync(id);
        if (existing is null)
        {
            return NotFound(CreateNotFoundResponse(id));
        }

        if (listing.UserId is not null &&
            !await _context.Users.AnyAsync(u => u.Id == listing.UserId))
        {
            return BadRequest(new ApiErrorResponse(
                $"User {listing.UserId} was not found."));
        }
        // Could use .SetValues instead of updating fields seperately
        existing.Title = listing.Title;
        existing.Description = listing.Description;
        existing.Price = listing.Price;
        existing.CreatorName = listing.CreatorName;
        existing.Category = listing.Category;
        existing.LocalPickupAvailable = listing.LocalPickupAvailable;
        existing.UserId = listing.UserId;
        
        await _context.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var existing = await _context.ArtworkListings.FindAsync(id);
        if (existing is null)
        {
            return NotFound(CreateNotFoundResponse(id));
        }
        _context.ArtworkListings.Remove(existing);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static ApiErrorResponse CreateNotFoundResponse(int id)
    {
        return new ApiErrorResponse($"Artwork listing {id} was not found.");
    }
}
