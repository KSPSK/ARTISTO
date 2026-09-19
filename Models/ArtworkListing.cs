using System.Text.Json.Serialization;

namespace ARTISTO.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ArtworkCategory
{
    Painting,
    Drawing,
    Photography,
    Sculpture,
    DigitalArt,
    Other
}

public class ArtworkListing
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string CreatorName { get; set; } = string.Empty;

    public ArtworkCategory Category { get; set; }

    public bool LocalPickupAvailable { get; set; }
}
