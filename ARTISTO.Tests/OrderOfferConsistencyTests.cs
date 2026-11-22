using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ARTISTO.Services;
using Xunit;

namespace ARTISTO.Tests;

public class OrderOfferConsistencyTests
{
    private static async Task<int> MakeOffer(HttpClient buyer, int listingId, decimal amount)
    {
        var response = await buyer.PostAsJsonAsync("/api/offers", new { artworkListingId = listingId, amount });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private static async Task<string?> Status(HttpClient client, string url)
    {
        return (await client.GetFromJsonAsync<JsonElement>(url)).GetProperty("status").GetString();
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

        var order = await directBuyer.PostAsJsonAsync("/api/orders", new { artworkListingId = listingId });

        Assert.Equal(HttpStatusCode.Created, order.StatusCode);
        Assert.Equal("Declined", await Status(offeringBuyer, $"/api/offers/{offerId}"));
        Assert.Equal(HttpStatusCode.BadRequest, (await seller.PostAsync($"/api/offers/{offerId}/accept", null)).StatusCode);
        Assert.Contains(app.Notifications.Sent, n => n.UserId == offeringBuyerId && n.Type == NotificationType.OfferDeclined);
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

        var ownOrder = await seller.PostAsJsonAsync("/api/orders", new { artworkListingId = listingId });

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
        var placed = await buyer.PostAsJsonAsync("/api/orders", new { artworkListingId = listingId });
        var orderId = (await placed.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var responses = await Task.WhenAll(
            seller.PostAsJsonAsync($"/api/orders/{orderId}/handover", new { }),
            buyer.PostAsJsonAsync($"/api/orders/{orderId}/cancel", new { }));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, response =>
            response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.BadRequest);
        var orderStatus = await Status(buyer, $"/api/orders/{orderId}");
        var listingStatus = await Status(buyer, $"/api/artworklistings/{listingId}");
        Assert.Equal(orderStatus == "Cancelled" ? "Available" : "Reserved", listingStatus);
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

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, response =>
            response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.BadRequest);
        var offerStatus = await Status(buyer, $"/api/offers/{offerId}");
        var sales = await seller.GetFromJsonAsync<JsonElement>("/api/orders?role=seller");
        Assert.Equal(offerStatus == "Accepted" ? 1 : 0, sales.GetArrayLength());
    }
}