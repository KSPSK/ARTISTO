using System.ComponentModel.DataAnnotations;

namespace ARTISTO.Models;

public sealed class ArtworkListingWriteRequest
{
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
