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
    public DbSet<CreatorProfile> CreatorProfiles => Set<CreatorProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CreatorProfile>()
            .HasMany(p => p.ArtworkListings)
            .WithOne(a => a.CreatorProfile)
            .HasForeignKey(a => a.CreatorProfileId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
