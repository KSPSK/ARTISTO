using ARTISTO.Models;
using Microsoft.AspNetCore.Http;

namespace ARTISTO.Services;

public sealed class ArtworkImageService : IArtworkImageService
{
    public const long MaximumFileSizeBytes = 5 * 1024 * 1024;

    public async Task<ArtworkImage> ReadAsync(
        int listingId,
        IFormFile image,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);

        if (image.Length <= 0)
        {
            throw new ArtworkImageValidationException("The image file is empty.");
        }

        if (image.Length > MaximumFileSizeBytes)
        {
            throw FileTooLarge();
        }

        var content = await ReadImageAsync(image, cancellationToken);
        return new ArtworkImage
        {
            ArtworkListingId = listingId,
            Content = content,
            ContentType = ArtworkImageFormatValidator.GetContentType(content)
        };
    }

    private static async Task<byte[]> ReadImageAsync(
        IFormFile image,
        CancellationToken cancellationToken)
    {
        await using var source = image.OpenReadStream();
        using var destination = new MemoryStream();
        var buffer = new byte[64 * 1024];

        while (true)
        {
            var bytesRead = await source.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            if (destination.Length + bytesRead > MaximumFileSizeBytes)
            {
                throw FileTooLarge();
            }

            await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }

        if (destination.Length == 0)
        {
            throw new ArtworkImageValidationException("The image file is empty.");
        }

        return destination.ToArray();
    }

    private static ArtworkImageValidationException FileTooLarge() =>
        new("The image must be 5 MB or smaller.");
}
