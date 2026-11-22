using ARTISTO.Data;
using ARTISTO.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace ARTISTO.Tests;

public sealed class PostgreSqlMigrationTests
{
    [PostgreSqlMigrationFact]
    public async Task Migrations_apply_to_an_empty_database_and_enforce_social_and_review_constraints()
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
            await using var db = new ArtistoDbContext(options);

            await db.Database.MigrateAsync();
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());

            var buyer = new User { Username = "buyer", DisplayName = "Buyer", Password = "unused", City = "Vilnius" };
            var seller = new User { Username = "seller", DisplayName = "Seller", Password = "unused", City = "Vilnius" };
            var listing = new ArtworkListing { Title = "Painting", CreatorName = seller.DisplayName, Price = 10, User = seller };
            var firstOrder = new Order { Buyer = buyer, Seller = seller, ArtworkListing = listing, Price = listing.Price, Status = OrderStatus.Completed };
            var secondOrder = new Order { Buyer = buyer, Seller = seller, ArtworkListing = listing, Price = listing.Price, Status = OrderStatus.Completed };
            db.Orders.AddRange(firstOrder, secondOrder);
            await db.SaveChangesAsync();

            await AssertSocialConstraints(db, buyer.Id, seller.Id, listing.Id);
            await AssertReviewConstraints(db, firstOrder, secondOrder);
        }
        finally
        {
            await using var drop = new NpgsqlCommand($"DROP DATABASE {databaseName}", admin);
            await drop.ExecuteNonQueryAsync();
        }
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
