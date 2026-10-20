using ARTISTO.Models;

namespace ARTISTO.Extensions;

public static class ArtworkListingExtensions
{
    // Filters listings in memory by category and maximum price.
    public static IEnumerable<ArtworkListing> Filter(
        this IEnumerable<ArtworkListing> listings,
        ArtworkCategory? category = null,
        decimal? maxPrice = null)
    {
        var query = listings.AsEnumerable();

        if (category.HasValue)
        {
            query = query.Where(item => item.Category == category.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(item => item.Price <= maxPrice.Value);
        }

        return query.OrderBy(item => item.Price).ToList();
    }
}
