using System.ComponentModel.DataAnnotations;

namespace ARTISTO.Models;

public class CreatorProfile
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    public ICollection<ArtworkListing> ArtworkListings { get; set; } = new List<ArtworkListing>();
}