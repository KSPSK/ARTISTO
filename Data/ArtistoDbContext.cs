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
    public DbSet<Offer> Offers => Set<Offer>();

    public DbSet<ArtworkLike> ArtworkLikes => Set<ArtworkLike>();
    public DbSet<ArtworkFavorite> ArtworkFavorites => Set<ArtworkFavorite>();
    public DbSet<UserFollow> UserFollows => Set<UserFollow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Offer>(entity =>
        {
            entity.ToTable("Offers", t => t.HasCheckConstraint("CK_Offers_Amount", "CAST(\"Amount\" AS numeric) BETWEEN 0.01 AND 1000000"));
            entity.Property(x => x.Amount).HasPrecision(9, 2);
            entity.HasIndex(x => new { x.BuyerId, x.ArtworkListingId }).IsUnique().HasFilter("\"Status\" = 0");
            entity.HasIndex(x => x.ArtworkListingId);
            entity.HasOne(x => x.Buyer).WithMany().HasForeignKey(x => x.BuyerId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ArtworkListing).WithMany().HasForeignKey(x => x.ArtworkListingId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ArtworkLike>(entity =>
        {
            entity.HasKey(x => new { x.UserId, x.ArtworkListingId });
            entity.HasIndex(x => x.ArtworkListingId);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ArtworkListing>().WithMany().HasForeignKey(x => x.ArtworkListingId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ArtworkFavorite>(entity =>
        {
            entity.HasKey(x => new { x.UserId, x.ArtworkListingId });
            entity.HasIndex(x => x.ArtworkListingId);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ArtworkListing>().WithMany().HasForeignKey(x => x.ArtworkListingId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<UserFollow>(entity =>
        {
            entity.ToTable("UserFollows", t => t.HasCheckConstraint("CK_UserFollows_NoSelfFollow", "\"FollowerId\" <> \"FollowedUserId\""));
            entity.HasKey(x => new { x.FollowerId, x.FollowedUserId });
            entity.HasIndex(x => x.FollowedUserId);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.FollowerId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.FollowedUserId).OnDelete(DeleteBehavior.Cascade);
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
