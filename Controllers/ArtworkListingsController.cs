using ARTISTO.Models;
using ARTISTO.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ARTISTO.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArtworkListingsController : ControllerBase
{
    // private static readonly object SyncRoot = new();

    // private static readonly List<ArtworkListing> Listings =
    // [
    //     new ArtworkListing
    //     {
    //         Id = 1,
    //         Title = "Evening in Vilnius",
    //         Description = "An original cityscape painting.",
    //         Price = 120.00m,
    //         CreatorName = "Geronimo",
    //         Category = ArtworkCategory.Painting,
    //         LocalPickupAvailable = true
    //     },
    //     new ArtworkListing
    //     {
    //         Id = 2,
    //         Title = "Forest Light",
    //         Description = "A small original landscape painting.",
    //         Price = 85.00m,
    //         CreatorName = "Geronimo",
    //         Category = ArtworkCategory.Painting,
    //         LocalPickupAvailable = false
    //     }
    // ];

    // private static int _nextId = 3;

    private readonly ArtistoDbContext _context;

    public ArtworkListingsController(ArtistoDbContext context)
    {
        _context = context;
    }

    // [HttpGet]
    // public ActionResult<IEnumerable<ArtworkListing>> GetAll([FromQuery] string? q)
    // {
    //     var query = q?.Trim();

    //     lock (SyncRoot)
    //     {
    //          IEnumerable<ArtworkListing> results = Listings;

    //         if (!string.IsNullOrEmpty(query))
    //         {
    //             results = results.Where(item =>
    //             (item.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
    //             (item.CreatorName?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false));
    //         }   

    //     return Ok(results.Select(CopyListing).ToList());
    //     }
    // }

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

    // [HttpGet("{id:int}")]
    // [ProducesResponseType<ArtworkListing>(StatusCodes.Status200OK)]
    // [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    // public ActionResult<ArtworkListing> GetById(int id)
    // {
    //     lock (SyncRoot)
    //     {
    //         var listing = Listings.FirstOrDefault(item => item.Id == id);

    //         return listing is null
    //             ? NotFound(CreateNotFoundResponse(id))
    //             : Ok(CopyListing(listing));
    //     }
    // }

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

    // [HttpPost]
    // public ActionResult<ArtworkListing> Create(ArtworkListing listing)
    // {
    //     lock (SyncRoot)
    //     {
    //         listing.Id = _nextId++;
    //         Listings.Add(CopyListing(listing));
    //     }

    //     var response = CopyListing(listing);

    //     return CreatedAtAction(
    //         nameof(GetById),
    //         new { id = response.Id },
    //         response);
    // }

    [HttpPost]
    public async Task<ActionResult<ArtworkListing>> Create(ArtworkListing listing)
    {
        _context.ArtworkListings.Add(listing);
        await _context.SaveChangesAsync();
        return CreatedAtAction(
            nameof(GetById),
            new { id = listing.Id },
            listing);
    }

    // [HttpPut("{id:int}")]
    // public ActionResult<ArtworkListing> Update(
    //     int id,
    //     ArtworkListing listing)
    // {
    //     lock (SyncRoot)
    //     {
    //         var index = Listings.FindIndex(item => item.Id == id);

    //         if (index < 0)
    //         {
    //             return NotFound(CreateNotFoundResponse(id));
    //         }

    //         listing.Id = id;
    //         var updatedListing = CopyListing(listing);
    //         Listings[index] = updatedListing;

    //         return Ok(CopyListing(updatedListing));
    //     }
    // }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ArtworkListing>> Update(int id, ArtworkListing listing)
    {
        var existing = await _context.ArtworkListings.FindAsync(id);
        if (existing is null)
        {
            return NotFound(CreateNotFoundResponse(id));
        }
        // Could use .SetValues instead of updating fields seperately
        existing.Title = listing.Title;
        existing.Description = listing.Description;
        existing.Price = listing.Price;
        existing.CreatorName = listing.CreatorName;
        existing.Category = listing.Category;
        existing.LocalPickupAvailable = listing.LocalPickupAvailable;
        await _context.SaveChangesAsync();
        return Ok(existing);
    }

    // [HttpDelete("{id:int}")]
    // public IActionResult Delete(int id)
    // {
    //     lock (SyncRoot)
    //     {
    //         var index = Listings.FindIndex(item => item.Id == id);

    //         if (index < 0)
    //         {
    //             return NotFound(CreateNotFoundResponse(id));
    //         }

    //         Listings.RemoveAt(index);
    //         return NoContent();
    //     }
    // }

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

    // private static ArtworkListing CopyListing(ArtworkListing listing)
    // {
    //     return new ArtworkListing
    //     {
    //         Id = listing.Id,
    //         Title = listing.Title,
    //         Description = listing.Description,
    //         Price = listing.Price,
    //         CreatorName = listing.CreatorName,
    //         Category = listing.Category,
    //         LocalPickupAvailable = listing.LocalPickupAvailable
    //     };
    // }

    private static ApiErrorResponse CreateNotFoundResponse(int id)
    {
        return new ApiErrorResponse($"Artwork listing {id} was not found.");
    }
}
