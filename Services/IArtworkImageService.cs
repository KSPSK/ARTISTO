using ARTISTO.Models;
using Microsoft.AspNetCore.Http;

namespace ARTISTO.Services;

public interface IArtworkImageService
{
    Task<ArtworkImage> ReadAsync(
        int listingId,
        IFormFile image,
        CancellationToken cancellationToken = default);
}
