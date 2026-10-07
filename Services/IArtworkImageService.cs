using Microsoft.AspNetCore.Http;

namespace ARTISTO.Services;

public interface IArtworkImageService
{
    Task<string> SaveAsync(
        IFormFile image,
        CancellationToken cancellationToken = default);

    void Delete(string? imageUrl);
}
