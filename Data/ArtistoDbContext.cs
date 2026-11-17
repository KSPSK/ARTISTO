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
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ArtworkLike> ArtworkLikes => Set<ArtworkLike>();
    public DbSet<ArtworkFavorite> ArtworkFavorites => Set<ArtworkFavorite>();
    public DbSet<UserFollow> UserFollows => Set<UserFollow>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.ToTable("Conversations", t => t.HasCheckConstraint("CK_Conversations_ParticipantsOrder", "\"UserOneId\" < \"UserTwoId\""));
            entity.HasIndex(x => new { x.UserOneId, x.UserTwoId }).IsUnique().HasFilter("\"ArtworkListingId\" IS NULL");
            entity.HasIndex(x => new { x.UserOneId, x.UserTwoId, x.ArtworkListingId }).IsUnique().HasFilter("\"ArtworkListingId\" IS NOT NULL");
            entity.HasIndex(x => x.UserTwoId);
            entity.HasIndex(x => x.ArtworkListingId);
            entity.HasOne(x => x.UserOne).WithMany().HasForeignKey(x => x.UserOneId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.UserTwo).WithMany().HasForeignKey(x => x.UserTwoId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ArtworkListing).WithMany().HasForeignKey(x => x.ArtworkListingId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("ChatMessages", t => t.HasCheckConstraint("CK_ChatMessages_Content", "LENGTH(TRIM(\"Content\")) BETWEEN 1 AND 2000"));
            entity.Property(x => x.Content).HasMaxLength(2000);
            entity.HasIndex(x => new { x.ConversationId, x.SentAtUtc });
            entity.HasIndex(x => x.SenderId);
            entity.HasOne(x => x.Conversation).WithMany(x => x.Messages).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Sender).WithMany().HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Offer>(entity =>
        {
            entity.ToTable("Offers", t =>
            {
                t.HasCheckConstraint("CK_Offers_Amount", "CAST(\"Amount\" AS numeric) BETWEEN 0.01 AND 1000000");
                t.HasCheckConstraint("CK_Offers_CounterAmount", "\"CounterAmount\" IS NULL OR CAST(\"CounterAmount\" AS numeric) BETWEEN 0.01 AND 1000000");
            });
            entity.Property(x => x.Amount).HasPrecision(9, 2);
            entity.Property(x => x.CounterAmount).HasPrecision(9, 2);
            entity.HasIndex(x => new { x.BuyerId, x.ArtworkListingId }).IsUnique().HasFilter("\"Status\" = 0");
            entity.HasIndex(x => x.ArtworkListingId);
            entity.HasOne(x => x.Buyer).WithMany().HasForeignKey(x => x.BuyerId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ArtworkListing).WithMany().HasForeignKey(x => x.ArtworkListingId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.ToTable("Reviews", t => t.HasCheckConstraint("CK_Reviews_Rating", "\"Rating\" BETWEEN 1 AND 5"));
            entity.HasIndex(x => x.OrderId).IsUnique();
            entity.HasIndex(x => x.SellerId);
            entity.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ArtworkListing).WithMany().HasForeignKey(x => x.ArtworkListingId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Reviewer).WithMany().HasForeignKey(x => x.ReviewerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Seller).WithMany().HasForeignKey(x => x.SellerId).OnDelete(DeleteBehavior.Restrict);
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
