using ARTISTO.Models;
using ARTISTO.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    [HttpPost]
    public async Task<ActionResult<ArtworkListing>> Create(ArtworkListing listing)
    {
        if (listing.CreatorProfileId is not null &&
            !await _context.CreatorProfiles.AnyAsync(p => p.Id == listing.CreatorProfileId))
        {
            return BadRequest(new ApiErrorResponse(
                $"Creator profile {listing.CreatorProfileId} was not found."));
        }

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

        if (listing.CreatorProfileId is not null &&
            !await _context.CreatorProfiles.AnyAsync(p => p.Id == listing.CreatorProfileId))
        {
            return BadRequest(new ApiErrorResponse(
                $"Creator profile {listing.CreatorProfileId} was not found."));
        }
        // Could use .SetValues instead of updating fields seperately
        existing.Title = listing.Title;
        existing.Description = listing.Description;
        existing.Price = listing.Price;
        existing.CreatorName = listing.CreatorName;
        existing.Category = listing.Category;
        existing.LocalPickupAvailable = listing.LocalPickupAvailable;
        existing.CreatorProfileId = listing.CreatorProfileId;
        
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
