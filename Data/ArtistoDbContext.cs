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
    public DbSet<User> Users => Set<User>();
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<ArtworkLike> ArtworkLikes => Set<ArtworkLike>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ArtworkLike>(entity =>
        {
            entity.HasKey(x => new { x.UserId, x.ArtworkListingId });
            entity.HasIndex(x => x.ArtworkListingId);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ArtworkListing>().WithMany().HasForeignKey(x => x.ArtworkListingId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasMany(u => u.ArtworkListings)
            .WithOne(a => a.User)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Buyer)
            .WithMany(u => u.Purchases)
            .HasForeignKey(o => o.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Seller)
            .WithMany(u => u.Sales)
            .HasForeignKey(o => o.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.ArtworkListing)
            .WithMany()
            .HasForeignKey(o => o.ArtworkListingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
