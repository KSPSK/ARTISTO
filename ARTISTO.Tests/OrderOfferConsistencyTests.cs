using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ARTISTO.Data;
using ARTISTO.Models;
using ARTISTO.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace ARTISTO.Tests;

public class OrderOfferConsistencyTests
{
    private static async Task<int> MakeOffer(
        HttpClient buyer,
        int listingId,
        decimal amount)
    {
        using var response = await buyer.PostAsJsonAsync(
            "/api/offers",
            new { artworkListingId = listingId, amount });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetInt32();
    }

    private static async Task<string?> Status(HttpClient client, string url)
    {
        var body = await client.GetFromJsonAsync<JsonElement>(url);
        return body.GetProperty("status").GetString();
    }

    [Fact]
    public async Task Buying_directly_declines_open_offers_and_notifies_their_buyers()
    {
        using var app = new SocialApp();
        await app.Seed();

        using var seller = app.Client();
        using var offeringBuyer = app.Client();
        using var directBuyer = app.Client();

        await app.Register(seller);
        var offeringBuyerId = await app.Register(offeringBuyer);
        await app.Register(directBuyer);

        var listingId = await app.Artwork(seller, localPickupAvailable: true);
        var offerId = await MakeOffer(offeringBuyer, listingId, 6);

        using var order = await directBuyer.PostAsJsonAsync(
            "/api/orders",
            new { artworkListingId = listingId });

        Assert.Equal(HttpStatusCode.Created, order.StatusCode);
        Assert.Equal("Declined", await Status(offeringBuyer, $"/api/offers/{offerId}"));

        using var accept = await seller.PostAsync($"/api/offers/{offerId}/accept", null);
        Assert.Equal(HttpStatusCode.BadRequest, accept.StatusCode);

        Assert.Contains(app.Notifications.Sent, notification =>
            notification.UserId == offeringBuyerId
            && notification.Type == NotificationType.OfferDeclined);
    }

    [Fact]
    public async Task Failed_direct_order_leaves_open_offers_untouched()
    {
        using var app = new SocialApp();
        await app.Seed();

        using var seller = app.Client();
        using var buyer = app.Client();

        await app.Register(seller);
        await app.Register(buyer);

        var listingId = await app.Artwork(seller, localPickupAvailable: true);
        var offerId = await MakeOffer(buyer, listingId, 6);

        using var ownOrder = await seller.PostAsJsonAsync(
            "/api/orders",
            new { artworkListingId = listingId });

        Assert.Equal(HttpStatusCode.BadRequest, ownOrder.StatusCode);
        Assert.Equal("Pending", await Status(buyer, $"/api/offers/{offerId}"));
    }

    [Fact]
    public async Task Handover_and_cancel_at_the_same_time_leave_a_consistent_order()
    {
        using var app = new SocialApp();
        await app.Seed();

        using var seller = app.Client();
        using var buyer = app.Client();

        await app.Register(seller);
        await app.Register(buyer);

        var listingId = await app.Artwork(seller, localPickupAvailable: true);

        using var placed = await buyer.PostAsJsonAsync(
            "/api/orders",
            new { artworkListingId = listingId });
        Assert.Equal(HttpStatusCode.Created, placed.StatusCode);

        var orderBody = await placed.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = orderBody.GetProperty("id").GetInt32();

        var responses = await Task.WhenAll(
            seller.PostAsJsonAsync($"/api/orders/{orderId}/handover", new { }),
            buyer.PostAsJsonAsync($"/api/orders/{orderId}/cancel", new { }));

        using var handover = responses[0];
        using var cancellation = responses[1];

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, response =>
            response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.BadRequest);

        var orderStatus = await Status(buyer, $"/api/orders/{orderId}");
        var listingStatus = await Status(buyer, $"/api/artworklistings/{listingId}");

        if (cancellation.StatusCode == HttpStatusCode.OK)
        {
            Assert.Equal("Cancelled", orderStatus);
            Assert.Equal("Available", listingStatus);
        }
        else
        {
            Assert.Equal("HandedOver", orderStatus);
            Assert.Equal("Reserved", listingStatus);
        }
    }

    [Fact]
    public async Task Accept_and_decline_at_the_same_time_leave_a_consistent_offer()
    {
        using var app = new SocialApp();
        await app.Seed();

        using var seller = app.Client();
        using var buyer = app.Client();

        await app.Register(seller);
        await app.Register(buyer);

        var listingId = await app.Artwork(seller, localPickupAvailable: true);
        var offerId = await MakeOffer(buyer, listingId, 6);

        var responses = await Task.WhenAll(
            seller.PostAsync($"/api/offers/{offerId}/accept", null),
            seller.PostAsync($"/api/offers/{offerId}/decline", null));

        using var acceptance = responses[0];
        using var decline = responses[1];

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, response =>
            response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.BadRequest);

        var offerStatus = await Status(buyer, $"/api/offers/{offerId}");
        var sales = await seller.GetFromJsonAsync<JsonElement>("/api/orders?role=seller");

        if (acceptance.StatusCode == HttpStatusCode.OK)
        {
            Assert.Equal("Accepted", offerStatus);
            Assert.Equal(1, sales.GetArrayLength());
        }
        else
        {
            Assert.Equal("Declined", offerStatus);
            Assert.Equal(0, sales.GetArrayLength());
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Late_offer_is_declined_and_buyer_is_notified(bool acceptExistingOffer)
    {
        var interceptor = new InsertLateOfferBeforeOrderSave();

        using var app = new SocialApp
        {
            Interceptors = [interceptor]
        };

        await app.Seed();

        using var seller = app.Client();
        using var buyer = app.Client();
        using var lateBuyer = app.Client();

        await app.Register(seller);
        await app.Register(buyer);
        var lateBuyerId = await app.Register(lateBuyer);

        var listingId = await app.Artwork(seller, localPickupAvailable: true);

        int? existingOfferId = acceptExistingOffer
            ? await MakeOffer(buyer, listingId, 6)
            : null;

        interceptor.LateOffer = new Offer
        {
            ArtworkListingId = listingId,
            BuyerId = lateBuyerId,
            Amount = 7m
        };

        using var order = existingOfferId is int offerId
            ? await seller.PostAsync($"/api/offers/{offerId}/accept", null)
            : await buyer.PostAsJsonAsync(
                "/api/orders",
                new { artworkListingId = listingId });

        Assert.Equal(
            acceptExistingOffer ? HttpStatusCode.OK : HttpStatusCode.Created,
            order.StatusCode);

        var lateOfferId = Assert.IsType<int>(interceptor.InsertedOfferId);

        Assert.Equal("Reserved", await Status(seller, $"/api/artworklistings/{listingId}"));
        Assert.Equal("Declined", await Status(lateBuyer, $"/api/offers/{lateOfferId}"));

        Assert.Contains(app.Notifications.Sent, notification =>
            notification.UserId == lateBuyerId
            && notification.Type == NotificationType.OfferDeclined);
    }

    [Fact]
    public async Task Offer_submitted_after_the_order_is_cancelled_stays_open()
    {
        using var app = new SocialApp();
        await app.Seed();

        using var seller = app.Client();
        using var directBuyer = app.Client();
        using var lateBuyer = app.Client();

        await app.Register(seller);
        await app.Register(directBuyer);
        await app.Register(lateBuyer);

        var listingId = await app.Artwork(seller, localPickupAvailable: true);

        using var order = await directBuyer.PostAsJsonAsync(
            "/api/orders",
            new { artworkListingId = listingId });
        Assert.Equal(HttpStatusCode.Created, order.StatusCode);

        var orderBody = await order.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = orderBody.GetProperty("id").GetInt32();

        using var cancelled = await directBuyer.PostAsJsonAsync(
            $"/api/orders/{orderId}/cancel",
            new { });
        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);

        var lateOfferId = await MakeOffer(lateBuyer, listingId, 6);

        Assert.Equal("Available", await Status(seller, $"/api/artworklistings/{listingId}"));
        Assert.Equal("Pending", await Status(lateBuyer, $"/api/offers/{lateOfferId}"));
    }

    private sealed class InsertLateOfferBeforeOrderSave : SaveChangesInterceptor
    {
        private int _ran;

        public Offer? LateOffer { get; set; }

        public int? InsertedOfferId { get; private set; }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is not ArtistoDbContext db || LateOffer is not { } offer)
            {
                return result;
            }

            var addsOrder = db.ChangeTracker.Entries<Order>()
                .Any(entry => entry.State == EntityState.Added);

            if (addsOrder && Interlocked.Exchange(ref _ran, 1) == 0)
            {
                // Use the current transaction so the late insert does not wait
                // for a second SQLite writer while this save is paused.
                var repository = new OfferRepository(db);
                InsertedOfferId = await repository.TryInsertAsync(offer, cancellationToken);
            }

            return result;
        }
    }
}