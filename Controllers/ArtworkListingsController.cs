using ARTISTO.Models;
using Microsoft.AspNetCore.Mvc;

namespace ARTISTO.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArtworkListingsController : ControllerBase
{
    private static readonly object SyncRoot = new();

    private static readonly List<ArtworkListing> Listings =
    [
        new ArtworkListing
        {
            Id = 1,
            Title = "Evening in Vilnius",
            Description = "An original cityscape painting.",
            Price = 120.00m,
            CreatorName = "Geronimo",
            Category = ArtworkCategory.Painting,
            LocalPickupAvailable = true
        },
        new ArtworkListing
        {
            Id = 2,
            Title = "Forest Light",
            Description = "A small original landscape painting.",
            Price = 85.00m,
            CreatorName = "Geronimo",
            Category = ArtworkCategory.Painting,
            LocalPickupAvailable = false
        }
    ];

    private static int _nextId = 3;

    [HttpGet]
    public ActionResult<IEnumerable<ArtworkListing>> GetAll([FromQuery] string? q)
    {
        var query = q?.Trim();

        lock (SyncRoot)
        {
             IEnumerable<ArtworkListing> results = Listings;

            if (!string.IsNullOrEmpty(query))
            {
                results = results.Where(item =>
                (item.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (item.CreatorName?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false));
            }   

        return Ok(results.Select(CopyListing).ToList());
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ArtworkListing>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public ActionResult<ArtworkListing> GetById(int id)
    {
        lock (SyncRoot)
        {
            var listing = Listings.FirstOrDefault(item => item.Id == id);

            return listing is null
                ? NotFound(CreateNotFoundResponse(id))
                : Ok(CopyListing(listing));
        }
    }

    [HttpPost]
    public ActionResult<ArtworkListing> Create(ArtworkListing listing)
    {
        lock (SyncRoot)
        {
            listing.Id = _nextId++;
            Listings.Add(CopyListing(listing));
        }

        var response = CopyListing(listing);

        return CreatedAtAction(
            nameof(GetById),
            new { id = response.Id },
            response);
    }

    [HttpPut("{id:int}")]
    public ActionResult<ArtworkListing> Update(
        int id,
        ArtworkListing listing)
    {
        lock (SyncRoot)
        {
            var index = Listings.FindIndex(item => item.Id == id);

            if (index < 0)
            {
                return NotFound(CreateNotFoundResponse(id));
            }

            listing.Id = id;
            var updatedListing = CopyListing(listing);
            Listings[index] = updatedListing;

            return Ok(CopyListing(updatedListing));
        }
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        lock (SyncRoot)
        {
            var index = Listings.FindIndex(item => item.Id == id);

            if (index < 0)
            {
                return NotFound(CreateNotFoundResponse(id));
            }

            Listings.RemoveAt(index);
            return NoContent();
        }
    }

    private static ArtworkListing CopyListing(ArtworkListing listing)
    {
        return new ArtworkListing
        {
            Id = listing.Id,
            Title = listing.Title,
            Description = listing.Description,
            Price = listing.Price,
            CreatorName = listing.CreatorName,
            Category = listing.Category,
            LocalPickupAvailable = listing.LocalPickupAvailable
        };
    }

    private static ApiErrorResponse CreateNotFoundResponse(int id)
    {
        return new ApiErrorResponse($"Artwork listing {id} was not found.");
    }
}
