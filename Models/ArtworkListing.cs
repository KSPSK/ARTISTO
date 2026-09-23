using System.ComponentModel.DataAnnotations;
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

    [Required]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 1000000)]
    public decimal Price { get; set; }

    [Required]
    public string CreatorName { get; set; } = string.Empty;

    public ArtworkCategory Category { get; set; }

    public bool LocalPickupAvailable { get; set; }
}
