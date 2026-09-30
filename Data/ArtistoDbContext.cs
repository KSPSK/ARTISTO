using ARTISTO.Models;
using Microsoft.EntityFrameworkCore;

namespace ARTISTO.Data;

public class ArtistoDbContext : DbContext
{
    public ArtistoDbContext(DbContextOptions<ArtistoDbContext> options)
        : base(options)
    {
    }

    public DbSet<ArtworkListing> ArtworkListings => Set<ArtworkListing>();
}
