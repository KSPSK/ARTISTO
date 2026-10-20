using ARTISTO.Models;
using Microsoft.AspNetCore.Http;

namespace ARTISTO.Services;

public interface IArtworkListingService
{
    Task<IEnumerable<ArtworkListing>> GetAllAsync(
        string? search = null,
        ArtworkCategory? category = null,
        decimal? maxPrice = null);

    Task<ArtworkListing?> GetByIdAsync(int id);

    Task<ServiceResult<ArtworkListing>> CreateAsync(ArtworkListingWriteRequest request, int userId);

    Task<ServiceResult<ArtworkListing>> UpdateAsync(int id, ArtworkListingWriteRequest request, int userId);

    Task<ServiceResult> DeleteAsync(int id, int userId);

    Task<ServiceResult<ArtworkListing>> UploadImageAsync(int id, IFormFile? image, int userId);

    Task<ServiceResult> DeleteImageAsync(int id, int userId);
}
