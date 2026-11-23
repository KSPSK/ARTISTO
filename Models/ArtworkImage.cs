namespace ARTISTO.Models;

public sealed class ArtworkImage
{
    public int ArtworkListingId { get; init; }
    public required byte[] Content { get; init; }
    public required string ContentType { get; init; }
}
