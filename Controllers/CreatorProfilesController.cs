using ARTISTO.Data;
using ARTISTO.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ARTISTO.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CreatorProfilesController : ControllerBase
{
    private readonly ArtistoDbContext _context;

    public CreatorProfilesController(ArtistoDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CreatorProfileResponse>>> GetAll()
    {
        var profiles = await _context.CreatorProfiles
            .AsNoTracking()
            .OrderBy(p => p.DisplayName)
            .ToListAsync();

        return Ok(profiles.Select(ToResponse));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<CreatorProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreatorProfileResponse>> GetById(int id)
    {
        var profile = await _context.CreatorProfiles.FindAsync(id);
        if (profile is null)
        {
            return NotFound(CreateNotFoundResponse(id));
        }
        return Ok(ToResponse(profile));
    }

    [HttpGet("{id:int}/artworklistings")]
    [ProducesResponseType<IEnumerable<ArtworkListing>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ArtworkListing>>> GetListings(int id)
    {
        var exists = await _context.CreatorProfiles.AnyAsync(p => p.Id == id);
        if (!exists)
        {
            return NotFound(CreateNotFoundResponse(id));
        }

        var listings = await _context.ArtworkListings
            .AsNoTracking()
            .Where(a => a.CreatorProfileId == id)
            .ToListAsync();

        return Ok(listings);
    }

    [HttpPost]
    public async Task<ActionResult<CreatorProfileResponse>> Create(CreatorProfileRequest request)
    {
        var profile = new CreatorProfile
        {
            DisplayName = request.DisplayName,
            Description = request.Description,
            City = request.City
        };

        _context.CreatorProfiles.Add(profile);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = profile.Id },
            ToResponse(profile));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<CreatorProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreatorProfileResponse>> Update(int id, CreatorProfileRequest request)
    {
        var existing = await _context.CreatorProfiles.FindAsync(id);
        if (existing is null)
        {
            return NotFound(CreateNotFoundResponse(id));
        }

        existing.DisplayName = request.DisplayName;
        existing.Description = request.Description;
        existing.City = request.City;
        await _context.SaveChangesAsync();

        return Ok(ToResponse(existing));
    }

    private static CreatorProfileResponse ToResponse(CreatorProfile profile)
    {
        return new CreatorProfileResponse
        {
            Id = profile.Id,
            DisplayName = profile.DisplayName,
            Description = profile.Description,
            City = profile.City
        };
    }

    private static ApiErrorResponse CreateNotFoundResponse(int id)
    {
        return new ApiErrorResponse($"Creator profile {id} was not found.");
    }
}