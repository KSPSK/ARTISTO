using System.ComponentModel.DataAnnotations;

namespace ARTISTO.Models;

public sealed record ArtworkListingWriteRequest
{
    [Required]
    public string Title { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; init; } = string.Empty;

    [Range(0.01, 1000000)]
    public decimal Price { get; init; }

    [Required]
    public string CreatorName { get; init; } = string.Empty;

    public ArtworkCategory Category { get; init; }

    public bool LocalPickupAvailable { get; init; }
}
