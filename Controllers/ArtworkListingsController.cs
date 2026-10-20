using ARTISTO.Data;
using ARTISTO.Extensions;
using ARTISTO.Models;
using ARTISTO.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ARTISTO.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArtworkListingsController : ControllerBase
{
    private readonly ArtistoDbContext _context;
    private readonly IArtworkImageService _images;

    public ArtworkListingsController(ArtistoDbContext context, IArtworkImageService images)
    {
        _context = context;
        _images = images;
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
        if (!User.TryGetUserId(out var userId) ||
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
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ArtworkListing>> Update(
        int id,
        ArtworkListingWriteRequest request)
    {
        if (!User.TryGetUserId(out var userId))
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

        if (existing.Status != ArtworkListingStatus.Available &&
            (request.Price != existing.Price ||
             request.LocalPickupAvailable != existing.LocalPickupAvailable))
        {
            return Conflict(new ApiErrorResponse(
                $"Price and local pickup cannot be changed while the listing is {existing.Status.ToString().ToLowerInvariant()}."));
        }

        existing.Title = request.Title;
        existing.Description = request.Description;
        existing.Price = request.Price;
        existing.CreatorName = request.CreatorName;
        existing.Category = request.Category;
        existing.LocalPickupAvailable = request.LocalPickupAvailable;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new ApiErrorResponse(
                "The listing was reserved or changed while you were editing it. Reload it and try again."));
        }

        return Ok(existing);
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        if (!User.TryGetUserId(out var userId))
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

        var hasOrders = await _context.Orders.AnyAsync(o => o.ArtworkListingId == id);
        if (hasOrders)
        {
            return BadRequest(new ApiErrorResponse("Cannot delete an artwork listing that has existing orders."));
        }

        var imageUrl = existing.ImageUrl;
        _context.ArtworkListings.Remove(existing);
        await _context.SaveChangesAsync();
        _images.Delete(imageUrl);
        return NoContent();
    }

    [Authorize]
    [HttpPost("{id:int}/image")]
    [ProducesResponseType<ArtworkListing>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArtworkListing>> UploadImage(int id, IFormFile? image)
    {
        if (!User.TryGetUserId(out var userId))
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

        if (image is null)
        {
            return BadRequest(new ApiErrorResponse("An image file is required."));
        }

        string imageUrl;
        try
        {
            imageUrl = await _images.SaveAsync(image);
        }
        catch (ArtworkImageValidationException exception)
        {
            return BadRequest(new ApiErrorResponse(exception.Message));
        }

        var previousImageUrl = existing.ImageUrl;
        existing.ImageUrl = imageUrl;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch
        {
            existing.ImageUrl = previousImageUrl;
            _images.Delete(imageUrl);
            throw;
        }

        _images.Delete(previousImageUrl);

        return Ok(existing);
    }

    [Authorize]
    [HttpDelete("{id:int}/image")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteImage(int id)
    {
        if (!User.TryGetUserId(out var userId))
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

        var imageUrl = existing.ImageUrl;
        existing.ImageUrl = null;
        await _context.SaveChangesAsync();
        _images.Delete(imageUrl);
        return NoContent();
    }

    private static ApiErrorResponse CreateNotFoundResponse(int id)
    {
        return new ApiErrorResponse($"Artwork listing {id} was not found.");
    }
}
