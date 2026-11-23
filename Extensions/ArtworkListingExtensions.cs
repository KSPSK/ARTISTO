using ARTISTO.Models;

namespace ARTISTO.Extensions;

public static class ArtworkListingExtensions
{
    // Appends category and maximum price filters to the EF Core SQL query.
    public static IQueryable<ArtworkListing> Filter(
        this IQueryable<ArtworkListing> query,
        ArtworkCategory? category = null,
        decimal? maxPrice = null)
    {
        if (category.HasValue)
        {
            query = query.Where(item => item.Category == category.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(item => item.Price <= maxPrice.Value);
        }
        
        if (category.HasValue || maxPrice.HasValue)
        {
            query = query.OrderBy(item => item.Price);
        }

        return query;
    }
}
