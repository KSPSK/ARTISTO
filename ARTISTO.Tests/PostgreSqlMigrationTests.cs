using ARTISTO.Data;
using ARTISTO.Models;
using ARTISTO.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace ARTISTO.Tests;

public sealed class PostgreSqlMigrationTests
{
    [PostgreSqlMigrationFact]
    public Task Migrated_database_enforces_constraints_and_serializes_reservation_with_offer_creation() =>
        WithTestDatabaseAsync(async options =>
        {
            await using var db = new ArtistoDbContext(options);

            await db.Database.MigrateAsync();
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());

            var buyer = new User { Username = "buyer", DisplayName = "Buyer", Password = "unused", City = "Vilnius" };
            var seller = new User { Username = "seller", DisplayName = "Seller", Password = "unused", City = "Vilnius" };
            var listing = new ArtworkListing { Title = "Painting", CreatorName = seller.DisplayName, Price = 10, User = seller, LocalPickupAvailable = true };
            var firstOrder = new Order { Buyer = buyer, Seller = seller, ArtworkListing = listing, Price = listing.Price, Status = OrderStatus.Completed };
            var secondOrder = new Order { Buyer = buyer, Seller = seller, ArtworkListing = listing, Price = listing.Price, Status = OrderStatus.Completed };
            db.Orders.AddRange(firstOrder, secondOrder);
            await db.SaveChangesAsync();

            await AssertSocialConstraints(db, buyer.Id, seller.Id, listing.Id);
            await AssertReviewConstraints(db, firstOrder, secondOrder);
            await AssertOfferWaitsForReservation(options, listing.Id, buyer.Id);
        });

    [PostgreSqlMigrationFact]
    public Task Artwork_image_migration_clears_legacy_urls_and_creates_cascading_storage() =>
        WithTestDatabaseAsync(async options =>
        {
            await using var db = new ArtistoDbContext(options);
            await db.GetService<IMigrator>().MigrateAsync("20261123105647_AddNotifications");
            var legacy = new ArtworkListing { Title = "Legacy", CreatorName = "Artist", Price = 10, ImageUrl = "/uploads/artworks/old.png" };
            var external = new ArtworkListing { Title = "External", CreatorName = "Artist", Price = 10, ImageUrl = "https://example.com/image.png" };
            db.ArtworkListings.AddRange(legacy, external);
            await db.SaveChangesAsync();

            await db.Database.MigrateAsync();
            db.ChangeTracker.Clear();
            Assert.Null((await db.ArtworkListings.FindAsync(legacy.Id))!.ImageUrl);
            Assert.Equal(external.ImageUrl, (await db.ArtworkListings.FindAsync(external.Id))!.ImageUrl);

            var images = new ArtworkListingRepository(db);
            var image = new ArtworkImage { ArtworkListingId = legacy.Id, Content = [1, 2, 3], ContentType = "image/png" };
            await images.SetImageAsync(image);
            await db.SaveChangesAsync();
            var replacement = new ArtworkImage { ArtworkListingId = legacy.Id, Content = [4, 5, 6], ContentType = "image/jpeg" };
            await images.SetImageAsync(replacement);
            var stored = await images.GetImageAsync(legacy.Id);
            Assert.Equal(replacement.Content, stored!.Content);
            Assert.Equal(replacement.ContentType, stored.ContentType);

            db.ChangeTracker.Clear();
            await db.ArtworkListings.Where(listing => listing.Id == legacy.Id).ExecuteDeleteAsync();
            Assert.Null(await images.GetImageAsync(legacy.Id));
        });

    private static async Task WithTestDatabaseAsync(Func<DbContextOptions<ArtistoDbContext>, Task> test)
    {
        var settings = new NpgsqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable(PostgreSqlMigrationFactAttribute.ConnectionVariable))
        {
            Pooling = false
        };
        await using var admin = new NpgsqlConnection(settings.ConnectionString);
        await admin.OpenAsync();

        var databaseName = $"artisto_migration_tests_{Guid.NewGuid():N}";
        await using var create = new NpgsqlCommand($"CREATE DATABASE {databaseName} TEMPLATE template0", admin);
        await create.ExecuteNonQueryAsync();

        try
        {
            settings.Database = databaseName;
            var options = new DbContextOptionsBuilder<ArtistoDbContext>()
                .UseNpgsql(settings.ConnectionString)
                .Options;
            await test(options);
        }
        finally
        {
            await using var drop = new NpgsqlCommand($"DROP DATABASE {databaseName}", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static async Task AssertOfferWaitsForReservation(
        DbContextOptions<ArtistoDbContext> options, int listingId, int buyerId)
    {
        await using var purchaseDb = new ArtistoDbContext(options);
        await using var purchase = await purchaseDb.Database.BeginTransactionAsync();
        await purchaseDb.LockListingAsync(listingId);
        var listing = await purchaseDb.ArtworkListings.SingleAsync(item => item.Id == listingId);
        listing.Status = ArtworkListingStatus.Reserved;
        await purchaseDb.SaveChangesAsync();

        await using var offerDb = new ArtistoDbContext(options);
        await offerDb.Database.OpenConnectionAsync();
        await offerDb.Database.ExecuteSqlRawAsync("SET lock_timeout = '1s'");
        var offers = new OfferRepository(offerDb);
        var notifications = new DatabaseNotificationService(offerDb);
        var orders = new OrderService(new OrderRepository(offerDb), offers, notifications, offerDb);
        var service = new OfferService(offers, orders, notifications, offerDb);
        var request = new CreateOfferRequest { ArtworkListingId = listingId, Amount = 6m };

        var error = await Assert.ThrowsAsync<PostgresException>(
            () => service.CreateOfferAsync(request, buyerId, CancellationToken.None));
        Assert.Equal(PostgresErrorCodes.LockNotAvailable, error.SqlState);

        await purchase.CommitAsync();

        var result = await service.CreateOfferAsync(request, buyerId, CancellationToken.None);
        Assert.Equal(ServiceStatus.Conflict, result.Status);
        Assert.False(await offerDb.Offers.AnyAsync(offer => offer.ArtworkListingId == listingId));
        Assert.False(await offerDb.Notifications.AnyAsync());
    }

    private static async Task AssertSocialConstraints(ArtistoDbContext db, int buyerId, int sellerId, int listingId)
    {
        FormattableString like = $"""
            INSERT INTO "ArtworkLikes" ("UserId", "ArtworkListingId") VALUES ({buyerId}, {listingId})
            """;
        FormattableString favorite = $"""
            INSERT INTO "ArtworkFavorites" ("UserId", "ArtworkListingId") VALUES ({buyerId}, {listingId})
            """;
        FormattableString follow = $"""
            INSERT INTO "UserFollows" ("FollowerId", "FollowedUserId") VALUES ({buyerId}, {sellerId})
            """;

        Assert.Equal(1, await db.Database.ExecuteSqlInterpolatedAsync(like));
        Assert.Equal(1, await db.Database.ExecuteSqlInterpolatedAsync(favorite));
        Assert.Equal(1, await db.Database.ExecuteSqlInterpolatedAsync(follow));

        await AssertConstraintViolation(db, like, PostgresErrorCodes.UniqueViolation, "PK_ArtworkLikes");
        await AssertConstraintViolation(db, favorite, PostgresErrorCodes.UniqueViolation, "PK_ArtworkFavorites");
        await AssertConstraintViolation(db, follow, PostgresErrorCodes.UniqueViolation, "PK_UserFollows");

        FormattableString selfFollow = $"""
            INSERT INTO "UserFollows" ("FollowerId", "FollowedUserId") VALUES ({buyerId}, {buyerId})
            """;
        await AssertConstraintViolation(db, selfFollow, PostgresErrorCodes.CheckViolation, "CK_UserFollows_NoSelfFollow");
    }

    private static async Task AssertReviewConstraints(ArtistoDbContext db, Order firstOrder, Order secondOrder)
    {
        Assert.Equal(1, await db.Database.ExecuteSqlInterpolatedAsync(InsertReview(firstOrder, 1)));
        await AssertConstraintViolation(db, InsertReview(firstOrder, 5), PostgresErrorCodes.UniqueViolation, "IX_Reviews_OrderId");

        await AssertConstraintViolation(db, InsertReview(secondOrder, 0), PostgresErrorCodes.CheckViolation, "CK_Reviews_Rating");
        await AssertConstraintViolation(db, InsertReview(secondOrder, 6), PostgresErrorCodes.CheckViolation, "CK_Reviews_Rating");
        Assert.Equal(1, await db.Database.ExecuteSqlInterpolatedAsync(InsertReview(secondOrder, 5)));
    }

    private static FormattableString InsertReview(Order order, int rating)
    {
        return $"""
            INSERT INTO "Reviews" ("OrderId", "ArtworkListingId", "ReviewerId", "SellerId", "Rating", "CreatedAtUtc")
            VALUES ({order.Id}, {order.ArtworkListingId}, {order.BuyerId}, {order.SellerId}, {rating}, CURRENT_TIMESTAMP)
            """;
    }

    private static async Task AssertConstraintViolation(ArtistoDbContext db, FormattableString sql, string sqlState, string constraintName)
    {
        var error = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync(sql));
        Assert.Equal(sqlState, error.SqlState);
        Assert.Equal(constraintName, error.ConstraintName);
    }
}

internal sealed class PostgreSqlMigrationFactAttribute : FactAttribute
{
    internal const string ConnectionVariable = "ARTISTO_TEST_POSTGRES";

    public PostgreSqlMigrationFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionVariable)))
        {
            Skip = $"Set {ConnectionVariable} to a PostgreSQL maintenance database connection with CREATE DATABASE permission.";
        }
    }
}
