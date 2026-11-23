using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;

namespace ARTISTO.Services;

internal static class ArtworkImageFormatValidator
{
    private const long MaximumPixelCount = 20_000_000;

    public static string GetExtension(byte[] data)
    {
        try
        {
            var format = Image.DetectFormat(data);
            var extension = format switch
            {
                JpegFormat => ".jpg",
                PngFormat => ".png",
                WebpFormat => ".webp",
                _ => null
            };

            if (extension is null)
            {
                throw InvalidImage();
            }

            var imageInfo = Image.Identify(data);
            var dimensions = new ImageDimensions(imageInfo.Width, imageInfo.Height);
            if (dimensions.PixelCount > MaximumPixelCount)
            {
                throw ImageTooLarge(dimensions);
            }

            using var image = Image.Load(data);
            return extension;
        }
        catch (ImageFormatException exception)
        {
            throw InvalidImage(exception);
        }
    }

    private static ArtworkImageValidationException InvalidImage(Exception? innerException = null) =>
        new("The image must be a valid JPEG, PNG, or WebP file.", innerException);

    private static ArtworkImageValidationException ImageTooLarge(ImageDimensions dimensions) =>
        new(
            $"The image dimensions are too large " +
            $"({dimensions.Width}x{dimensions.Height}). " +
            $"Images may contain at most {MaximumPixelCount:N0} pixels.");
}
