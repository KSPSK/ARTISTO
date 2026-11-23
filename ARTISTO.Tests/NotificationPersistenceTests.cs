using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ARTISTO.Data;
using ARTISTO.Models;
using ARTISTO.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ARTISTO.Tests;

public class NotificationPersistenceTests
{
    [Fact]
    public async Task Creating_an_offer_saves_a_notification_for_the_seller()
    {
        using var app = new SocialApp
        {
            UseDatabaseNotifications = true
        };

        await app.Seed();

        using var seller = app.Client();
        using var buyer = app.Client();

        var sellerId = await app.Register(seller);
        var buyerId = await app.Register(buyer);

        var listingId = await app.Artwork(
            seller,
            localPickupAvailable: true);

        using var response = await buyer.PostAsJsonAsync(
            "/api/offers",
            new
            {
                artworkListingId = listingId,
                amount = 6
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var offerId = body.GetProperty("id").GetInt32();

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<ArtistoDbContext>();

        var offer = await db.Offers
            .AsNoTracking()
            .SingleAsync(entry => entry.Id == offerId);

        Assert.Equal(buyerId, offer.BuyerId);
        Assert.Equal(listingId, offer.ArtworkListingId);
        Assert.Equal(6m, offer.Amount);

        var notification = Assert.Single(
            await db.Notifications.AsNoTracking().ToListAsync());

        Assert.Equal(sellerId, notification.UserId);
        Assert.Equal(NotificationType.OfferReceived, notification.Type);
        Assert.Equal("/offers", notification.Link);
        Assert.False(string.IsNullOrWhiteSpace(notification.Message));
        Assert.Null(notification.ReadAtUtc);
        Assert.NotEqual(default(DateTime), notification.CreatedAtUtc);
    }

    [Fact]
    public async Task Notification_save_failure_rolls_back_the_offer()
    {
        var interceptor = new FailNotificationSave();

        using var app = new SocialApp
        {
            UseDatabaseNotifications = true,
            Interceptors = [interceptor]
        };

        await app.Seed();

        using var seller = app.Client();
        using var buyer = app.Client();

        await app.Register(seller);
        await app.Register(buyer);

        var listingId = await app.Artwork(
            seller,
            localPickupAvailable: true);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            async () =>
            {
                using var response = await buyer.PostAsJsonAsync(
                    "/api/offers",
                    new
                    {
                        artworkListingId = listingId,
                        amount = 6
                    });
            });

        Assert.Equal(
            "Simulated notification save failure.",
            error.Message);

        Assert.True(interceptor.WasTriggered);

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<ArtistoDbContext>();

        Assert.False(await db.Offers.AnyAsync());
        Assert.False(await db.Notifications.AnyAsync());

        var listing = await db.ArtworkListings
            .AsNoTracking()
            .SingleAsync(entry => entry.Id == listingId);

        Assert.Equal(ArtworkListingStatus.Available, listing.Status);
    }

    private sealed class FailNotificationSave : SaveChangesInterceptor
    {
        public bool WasTriggered { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var addsNotification = eventData.Context?
                .ChangeTracker
                .Entries<Notification>()
                .Any(entry => entry.State == EntityState.Added) == true;

            if (addsNotification)
            {
                WasTriggered = true;

                throw new InvalidOperationException(
                    "Simulated notification save failure.");
            }

            return base.SavingChangesAsync(
                eventData,
                result,
                cancellationToken);
        }
    }
}