using Microsoft.AspNetCore.Http;

namespace ARTISTO.Services;

public sealed class ArtworkImageService : IArtworkImageService
{
    public const long MaximumFileSizeBytes = 5 * 1024 * 1024;

    private const string PublicUrlPrefix = "/uploads/artworks/";
    private readonly string _uploadDirectory;

    public ArtworkImageService(IWebHostEnvironment environment)
    {
        var webRoot = string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? Path.Combine(environment.ContentRootPath, "wwwroot")
            : environment.WebRootPath;

        _uploadDirectory = Path.Combine(webRoot, "uploads", "artworks");
        Directory.CreateDirectory(_uploadDirectory);
    }

    public async Task<string> SaveAsync(
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

        var imageData = await ReadImageAsync(image, cancellationToken);
        var extension = ArtworkImageFormatValidator.GetExtension(imageData);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var destination = Path.Combine(_uploadDirectory, fileName);
        var temporary = Path.Combine(_uploadDirectory, $".{Guid.NewGuid():N}.tmp");

        try
        {
            await File.WriteAllBytesAsync(temporary, imageData, cancellationToken);
            File.Move(temporary, destination);
        }
        finally
        {
            File.Delete(temporary);
        }

        return $"{PublicUrlPrefix}{fileName}";
    }

    public void Delete(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return;
        }

        File.Delete(GetManagedImagePath(imageUrl));
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

    private string GetManagedImagePath(string imageUrl)
    {
        if (!imageUrl.StartsWith(PublicUrlPrefix, StringComparison.Ordinal))
        {
            throw new ArgumentException("The image URL is not managed by this service.", nameof(imageUrl));
        }

        var fileName = imageUrl[PublicUrlPrefix.Length..];
        var extension = Path.GetExtension(fileName);
        var name = Path.GetFileNameWithoutExtension(fileName);

        if (fileName != Path.GetFileName(fileName)
            || !Guid.TryParseExact(name, "N", out _)
            || extension is not (".jpg" or ".png" or ".webp"))
        {
            throw new ArgumentException("The image URL is not managed by this service.", nameof(imageUrl));
        }

        return Path.Combine(_uploadDirectory, fileName);
    }

    private static ArtworkImageValidationException FileTooLarge() =>
        new("The image must be 5 MB or smaller.");
}
