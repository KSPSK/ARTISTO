using ARTISTO.Data;
using ARTISTO.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ARTISTO.Services;

public class ArtworkListingService : IArtworkListingService
{
    private readonly IArtworkListingRepository _repository;
    private readonly IArtworkImageService _images;
    private readonly ArtistoDbContext _db;

    public ArtworkListingService(IArtworkListingRepository repository, IArtworkImageService images, ArtistoDbContext db)
    {
        _repository = repository;
        _images = images;
        _db = db;
    }

    public async Task<IEnumerable<ArtworkListing>> GetAllAsync(
        string? search = null,
        ArtworkCategory? category = null,
        decimal? maxPrice = null)
    {
        return await _repository.GetAllAsync(search, category, maxPrice);
    }

    public async Task<ArtworkListing?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public Task<ArtworkImage?> GetImageAsync(int id)
    {
        return _repository.GetImageAsync(id);
    }

    public async Task<ServiceResult<ArtworkListing>> CreateAsync(
        ArtworkListingWriteRequest request,
        int userId)
    {
        if (!await _repository.UserExistsAsync(userId))
        {
            return ServiceResult<ArtworkListing>.Unauthorized(
                "A valid user account is required to create a listing.");
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

        await _repository.AddAsync(listing);
        await _repository.SaveChangesAsync();

        return ServiceResult<ArtworkListing>.Success(listing);
    }

    public async Task<ServiceResult<ArtworkListing>> UpdateAsync(
        int id,
        ArtworkListingWriteRequest request,
        int userId)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
        {
            return ServiceResult<ArtworkListing>.NotFound($"Artwork listing {id} was not found.");
        }

        if (existing.UserId is null || existing.UserId != userId)
        {
            return ServiceResult<ArtworkListing>.Forbidden();
        }

        if (existing.Status != ArtworkListingStatus.Available &&
            (request.Price != existing.Price ||
             request.LocalPickupAvailable != existing.LocalPickupAvailable))
        {
            return ServiceResult<ArtworkListing>.Conflict(
                $"Price and local pickup cannot be changed while the listing is {existing.Status.ToString().ToLowerInvariant()}.");
        }

        existing.Title = request.Title;
        existing.Description = request.Description;
        existing.Price = request.Price;
        existing.CreatorName = request.CreatorName;
        existing.Category = request.Category;
        existing.LocalPickupAvailable = request.LocalPickupAvailable;

        try
        {
            await _repository.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult<ArtworkListing>.Conflict(
                "The listing was reserved or changed while you were editing it. Reload it and try again.");
        }

        return ServiceResult<ArtworkListing>.Success(existing);
    }

    public async Task<ServiceResult> DeleteAsync(int id, int userId)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
        {
            return ServiceResult.NotFound($"Artwork listing {id} was not found.");
        }

        if (existing.UserId is null || existing.UserId != userId)
        {
            return ServiceResult.Forbidden();
        }

        var hasOrders = await _repository.HasOrdersAsync(id);
        if (hasOrders)
        {
            return ServiceResult.BadRequest("Cannot delete an artwork listing that has existing orders.");
        }

        _repository.Remove(existing);
        await _repository.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<ArtworkListing>> UploadImageAsync(
        int id,
        IFormFile? image,
        int userId)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        await _db.LockListingAsync(id);

        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
        {
            return ServiceResult<ArtworkListing>.NotFound($"Artwork listing {id} was not found.");
        }

        if (existing.UserId is null || existing.UserId != userId)
        {
            return ServiceResult<ArtworkListing>.Forbidden();
        }

        if (image is null)
        {
            return ServiceResult<ArtworkListing>.BadRequest("An image file is required.");
        }

        ArtworkImage artworkImage;
        try
        {
            artworkImage = await _images.ReadAsync(id, image);
        }
        catch (ArtworkImageValidationException exception)
        {
            return ServiceResult<ArtworkListing>.BadRequest(exception.Message);
        }

        await _repository.SetImageAsync(artworkImage);
        existing.ImageUrl = $"/api/artworklistings/{id}/image?v={Guid.NewGuid():N}";
        await _repository.SaveChangesAsync();
        await transaction.CommitAsync();

        return ServiceResult<ArtworkListing>.Success(existing);
    }

    public async Task<ServiceResult> DeleteImageAsync(int id, int userId)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        await _db.LockListingAsync(id);

        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
        {
            return ServiceResult.NotFound($"Artwork listing {id} was not found.");
        }

        if (existing.UserId is null || existing.UserId != userId)
        {
            return ServiceResult.Forbidden();
        }

        await _repository.RemoveImageAsync(id);
        existing.ImageUrl = null;
        await _repository.SaveChangesAsync();
        await transaction.CommitAsync();

        return ServiceResult.Success();
    }
}
