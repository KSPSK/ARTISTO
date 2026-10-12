namespace ARTISTO.Services;

public readonly struct ImageDimensions
{
    public int Width { get; }
    public int Height { get; }
    public long PixelCount => (long)Width * Height;

    public ImageDimensions(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width),
                "Image width must be greater than zero.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(height),
                "Image height must be greater than zero.");
        }

        Width = width;
        Height = height;
    }
}
