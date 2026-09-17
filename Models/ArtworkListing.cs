namespace ARTISTO.Models;

public class ArtworkListing
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string CreatorName { get; set; } = string.Empty;
}