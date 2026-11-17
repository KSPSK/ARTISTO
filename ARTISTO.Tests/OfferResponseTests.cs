using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using ARTISTO.Services;

namespace ARTISTO.Tests;

public class OfferResponseTests
{
    private static async Task<int> CreateListing(HttpClient seller, decimal price = 100)
    {
        var response = await seller.PostAsJsonAsync("/api/artworklistings", new
        {
            title = "Offer response artwork",
            description = "Test",
            price,
            creatorName = "Test",
            category = "Painting",
            localPickupAvailable = true
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private static async Task<int> MakeOffer(HttpClient buyer, int listingId, decimal amount)
    {
        var response = await buyer.PostAsJsonAsync("/api/offers", new { artworkListingId = listingId, amount });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private static Task<HttpResponseMessage> Respond(HttpClient client, int offerId, string action)
    {
        return client.PostAsync($"/api/offers/{offerId}/{action}", null);
    }

    private static Task<HttpResponseMessage> Counter(HttpClient seller, int offerId, decimal amount, string? message = null)
    {
        return seller.PostAsJsonAsync($"/api/offers/{offerId}/counter", new { amount, message });
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Seller_accepts_offer_and_order_is_created_at_the_offer_price()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var seller = app.Client();
        using var buyer = app.Client();
        var sellerId = await app.Register(seller);
        var buyerId = await app.Register(buyer);
        var listingId = await CreateListing(seller);
        var offerId = await MakeOffer(buyer, listingId, 60);

        var response = await Respond(seller, offerId, "accept");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var offer = await Json(response);
        Assert.Equal("Accepted", offer.GetProperty("status").GetString());
        var orderId = offer.GetProperty("orderId").GetInt32();

        var order = await Json(await buyer.GetAsync($"/api/orders/{orderId}"));
        Assert.Equal(60m, order.GetProperty("price").GetDecimal());
        Assert.Equal("Pending", order.GetProperty("status").GetString());

        var listing = await Json(await buyer.GetAsync($"/api/artworklistings/{listingId}"));
        Assert.Equal("Reserved", listing.GetProperty("status").GetString());
        
        Assert.Contains(app.Notifications.Sent, n => n.UserId == sellerId && n.Type == NotificationType.OfferReceived);
        Assert.Contains(app.Notifications.Sent, n => n.UserId == buyerId && n.Type == NotificationType.OfferAccepted);
    }

    [Fact]
    public async Task Only_the_seller_can_accept_or_decline_a_pending_offer()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var seller = app.Client();
        using var buyer = app.Client();
        using var stranger = app.Client();
        await app.Register(seller);
        await app.Register(buyer);
        await app.Register(stranger);
        var offerId = await MakeOffer(buyer, await CreateListing(seller), 60);

        Assert.Equal(HttpStatusCode.Forbidden, (await Respond(buyer, offerId, "accept")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Respond(stranger, offerId, "accept")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Respond(buyer, offerId, "decline")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Counter(buyer, offerId, 80)).StatusCode);
    }

    [Fact]
    public async Task Seller_can_decline_an_offer_and_the_listing_stays_available()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var seller = app.Client();
        using var buyer = app.Client();
        await app.Register(seller);
        var buyerId = await app.Register(buyer);
        var listingId = await CreateListing(seller);
        var offerId = await MakeOffer(buyer, listingId, 60);

        var response = await Respond(seller, offerId, "decline");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Declined", (await Json(response)).GetProperty("status").GetString());
        var listing = await Json(await buyer.GetAsync($"/api/artworklistings/{listingId}"));
        Assert.Equal("Available", listing.GetProperty("status").GetString());
        Assert.Contains(app.Notifications.Sent, n => n.UserId == buyerId && n.Type == NotificationType.OfferDeclined);
    }

    [Fact]
    public async Task Buyer_accepts_counter_offer_and_order_uses_the_counter_price()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var seller = app.Client();
        using var buyer = app.Client();
        var sellerId = await app.Register(seller);
        var buyerId = await app.Register(buyer);
        var offerId = await MakeOffer(buyer, await CreateListing(seller), 60);

        var countered = await Counter(seller, offerId, 80, "  Lowest I can go.  ");

        Assert.Equal(HttpStatusCode.OK, countered.StatusCode);
        var counterBody = await Json(countered);
        Assert.Equal("Countered", counterBody.GetProperty("status").GetString());
        Assert.Equal(80m, counterBody.GetProperty("counterAmount").GetDecimal());
        Assert.Equal("Lowest I can go.", counterBody.GetProperty("counterMessage").GetString());
        Assert.Contains(app.Notifications.Sent, n => n.UserId == buyerId && n.Type == NotificationType.OfferCountered);

        Assert.Equal(HttpStatusCode.Forbidden, (await Respond(seller, offerId, "accept")).StatusCode);

        var accepted = await Respond(buyer, offerId, "accept");

        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        var orderId = (await Json(accepted)).GetProperty("orderId").GetInt32();
        var order = await Json(await seller.GetAsync($"/api/orders/{orderId}"));
        Assert.Equal(80m, order.GetProperty("price").GetDecimal());
        Assert.Contains(app.Notifications.Sent, n => n.UserId == sellerId && n.Type == NotificationType.OfferAccepted);
    }

    [Fact]
    public async Task Buyer_can_decline_a_counter_offer()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var seller = app.Client();
        using var buyer = app.Client();
        var sellerId = await app.Register(seller);
        await app.Register(buyer);
        var offerId = await MakeOffer(buyer, await CreateListing(seller), 60);
        Assert.Equal(HttpStatusCode.OK, (await Counter(seller, offerId, 80)).StatusCode);

        var response = await Respond(buyer, offerId, "decline");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Declined", (await Json(response)).GetProperty("status").GetString());
        Assert.Contains(app.Notifications.Sent, n => n.UserId == sellerId && n.Type == NotificationType.OfferDeclined);
    }

    [Theory]
    [InlineData("60")]
    [InlineData("50")]
    [InlineData("101")]
    [InlineData("80.555")]
    public async Task Counter_offer_must_be_above_the_offer_and_not_above_the_price(string amount)
    {
        using var app = new SocialApp();
        await app.Seed();
        using var seller = app.Client();
        using var buyer = app.Client();
        await app.Register(seller);
        await app.Register(buyer);
        var offerId = await MakeOffer(buyer, await CreateListing(seller, price: 100), 60);

        var response = await Counter(seller, offerId, decimal.Parse(amount, CultureInfo.InvariantCulture));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Closed_offers_cannot_be_answered_again()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var seller = app.Client();
        using var buyer = app.Client();
        await app.Register(seller);
        await app.Register(buyer);
        var offerId = await MakeOffer(buyer, await CreateListing(seller), 60);
        Assert.Equal(HttpStatusCode.OK, (await Respond(seller, offerId, "decline")).StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, (await Respond(seller, offerId, "accept")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Respond(seller, offerId, "decline")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Counter(seller, offerId, 80)).StatusCode);
    }

    [Fact]
    public async Task Accepting_one_offer_declines_the_other_open_offers()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var seller = app.Client();
        using var firstBuyer = app.Client();
        using var secondBuyer = app.Client();
        await app.Register(seller);
        var firstBuyerId = await app.Register(firstBuyer);
        await app.Register(secondBuyer);
        var listingId = await CreateListing(seller);
        var firstOfferId = await MakeOffer(firstBuyer, listingId, 60);
        var secondOfferId = await MakeOffer(secondBuyer, listingId, 70);

        Assert.Equal(HttpStatusCode.OK, (await Respond(seller, secondOfferId, "accept")).StatusCode);

        var firstOffer = await Json(await firstBuyer.GetAsync($"/api/offers/{firstOfferId}"));
        Assert.Equal("Declined", firstOffer.GetProperty("status").GetString());
        Assert.Contains(app.Notifications.Sent, n => n.UserId == firstBuyerId && n.Type == NotificationType.OfferDeclined);
    }

    [Fact]
    public async Task Offers_are_listed_for_the_buyer_and_the_seller_only()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var seller = app.Client();
        using var buyer = app.Client();
        using var stranger = app.Client();
        await app.Register(seller);
        await app.Register(buyer);
        await app.Register(stranger);
        var offerId = await MakeOffer(buyer, await CreateListing(seller), 60);

        var received = await Json(await seller.GetAsync("/api/offers?role=seller"));
        var sent = await Json(await buyer.GetAsync("/api/offers?role=buyer"));
        var strangers = await Json(await stranger.GetAsync("/api/offers"));

        Assert.Equal(offerId, Assert.Single(received.EnumerateArray()).GetProperty("id").GetInt32());
        Assert.Equal(offerId, Assert.Single(sent.EnumerateArray()).GetProperty("id").GetInt32());
        Assert.Empty(strangers.EnumerateArray());
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync($"/api/offers/{offerId}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await buyer.GetAsync("/api/offers?role=admin")).StatusCode);
    }

    [Fact]
    public async Task Order_events_notify_the_other_party()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var seller = app.Client();
        using var buyer = app.Client();
        var sellerId = await app.Register(seller);
        var buyerId = await app.Register(buyer);

        var placed = await buyer.PostAsJsonAsync("/api/orders", new { artworkListingId = await CreateListing(seller) });
        var orderId = (await Json(placed)).GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.OK, (await seller.PostAsync($"/api/orders/{orderId}/handover", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await buyer.PostAsync($"/api/orders/{orderId}/complete", null)).StatusCode);

        var cancelled = await buyer.PostAsJsonAsync("/api/orders", new { artworkListingId = await CreateListing(seller) });
        var cancelledId = (await Json(cancelled)).GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.OK, (await buyer.PostAsJsonAsync($"/api/orders/{cancelledId}/cancel", new { reason = "Changed my mind" })).StatusCode);

        Assert.Contains(app.Notifications.Sent, n => n.UserId == sellerId && n.Type == NotificationType.OrderPlaced);
        Assert.Contains(app.Notifications.Sent, n => n.UserId == buyerId && n.Type == NotificationType.OrderHandedOver);
        Assert.Contains(app.Notifications.Sent, n => n.UserId == sellerId && n.Type == NotificationType.OrderCompleted);
        Assert.Contains(app.Notifications.Sent, n => n.UserId == sellerId && n.Type == NotificationType.OrderCancelled);
    }
}