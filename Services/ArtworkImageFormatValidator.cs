using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;

namespace ARTISTO.Services;

internal static class ArtworkImageFormatValidator
{
    public static string GetExtension(ReadOnlySpan<byte> data)
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

            using var image = Image.Load(data);
            return extension;
        }
        catch (ImageFormatException)
        {
            throw InvalidImage();
        }
    }

    private static ArtworkImageValidationException InvalidImage() =>
        new("The image must be a valid JPEG, PNG, or WebP file.");
}
