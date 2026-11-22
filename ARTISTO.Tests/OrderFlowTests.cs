using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ARTISTO.Tests;

// End-to-end flows through the API: offer, order, handover, receipt and review.
public class OrderFlowTests
{
    private static async Task<JsonElement> Json(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static Task<HttpResponseMessage> PlaceOrder(HttpClient buyer, int listingId)
    {
        return buyer.PostAsJsonAsync("/api/orders", new { artworkListingId = listingId });
    }

    private static async Task<int> Order(HttpClient buyer, int listingId)
    {
        var response = await PlaceOrder(buyer, listingId);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await Json(response)).GetProperty("id").GetInt32();
    }

    private static Task<HttpResponseMessage> Act(HttpClient client, int orderId, string action)
    {
        return client.PostAsJsonAsync($"/api/orders/{orderId}/{action}", new { });
    }

    private static async Task<string?> OrderStatus(HttpClient client, int orderId)
    {
        return (await Json(await client.GetAsync($"/api/orders/{orderId}"))).GetProperty("status").GetString();
    }

    private static async Task<string?> ListingStatus(HttpClient client, int listingId)
    {
        return (await Json(await client.GetAsync($"/api/artworklistings/{listingId}"))).GetProperty("status").GetString();
    }

    private static async Task<int> MakeOffer(HttpClient buyer, int listingId, decimal amount)
    {
        var response = await buyer.PostAsJsonAsync("/api/offers", new { artworkListingId = listingId, amount });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await Json(response)).GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task Offer_counter_order_handover_receipt_and_review_work_end_to_end()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var seller = app.Client();
        using var buyer = app.Client();
        var sellerId = await app.Register(seller);
        await app.Register(buyer);
        var listingId = await app.Artwork(seller, localPickupAvailable: true);
        var offerId = await MakeOffer(buyer, listingId, 6);

        var countered = await seller.PostAsJsonAsync($"/api/offers/{offerId}/counter", new { amount = 8 });
        Assert.Equal(HttpStatusCode.OK, countered.StatusCode);

        var accepted = await buyer.PostAsync($"/api/offers/{offerId}/accept", null);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        var orderId = (await Json(accepted)).GetProperty("orderId").GetInt32();

        var order = await Json(await buyer.GetAsync($"/api/orders/{orderId}"));
        Assert.Equal(8m, order.GetProperty("price").GetDecimal());
        Assert.Equal("Pending", order.GetProperty("status").GetString());
        Assert.Equal("Reserved", await ListingStatus(buyer, listingId));

        Assert.Equal(HttpStatusCode.OK, (await Act(seller, orderId, "handover")).StatusCode);
        Assert.Equal("HandedOver", await OrderStatus(buyer, orderId));

        Assert.Equal(HttpStatusCode.OK, (await Act(buyer, orderId, "complete")).StatusCode);
        Assert.Equal("Completed", await OrderStatus(seller, orderId));
        Assert.Equal("Sold", await ListingStatus(buyer, listingId));

        var review = await buyer.PostAsJsonAsync("/api/reviews", new { orderId, rating = 5, comment = "Great" });
        Assert.Equal(HttpStatusCode.Created, review.StatusCode);

        var reviews = await Json(await buyer.GetAsync($"/api/users/{sellerId}/reviews"));
        Assert.Equal(1, reviews.GetProperty("reviewCount").GetInt32());
        Assert.Equal(5, reviews.GetProperty("averageRating").GetDouble());
    }

    [Fact]
    public async Task Order_steps_must_happen_in_order_and_by_the_right_person()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var seller = app.Client();
        using var buyer = app.Client();
        await app.Register(seller);
        await app.Register(buyer);
        var listingId = await app.Artwork(seller, localPickupAvailable: true);
        var orderId = await Order(buyer, listingId);

        Assert.Equal(HttpStatusCode.BadRequest, (await Act(buyer, orderId, "complete")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Act(buyer, orderId, "handover")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await Act(seller, orderId, "handover")).StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, (await Act(seller, orderId, "handover")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Act(buyer, orderId, "cancel")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Act(seller, orderId, "complete")).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await Act(buyer, orderId, "complete")).StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, (await Act(buyer, orderId, "complete")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Act(seller, orderId, "cancel")).StatusCode);
        Assert.Equal("Sold", await ListingStatus(buyer, listingId));
    }

    [Fact]
    public async Task Cancelled_order_frees_the_artwork_for_another_buyer()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var seller = app.Client();
        using var buyer = app.Client();
        using var secondBuyer = app.Client();
        await app.Register(seller);
        await app.Register(buyer);
        await app.Register(secondBuyer);
        var listingId = await app.Artwork(seller, localPickupAvailable: true);
        var orderId = await Order(buyer, listingId);

        Assert.Equal(HttpStatusCode.Conflict, (await PlaceOrder(secondBuyer, listingId)).StatusCode);

        var cancelled = await buyer.PostAsJsonAsync($"/api/orders/{orderId}/cancel", new { reason = "  Changed my mind  " });
        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        Assert.Equal("Changed my mind", (await Json(cancelled)).GetProperty("cancellationReason").GetString());
        Assert.Equal("Available", await ListingStatus(buyer, listingId));

        Assert.Equal(HttpStatusCode.BadRequest, (await Act(seller, orderId, "handover")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await PlaceOrder(secondBuyer, listingId)).StatusCode);
    }

    [Fact]
    public async Task Parallel_orders_for_the_same_artwork_create_only_one_order()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var seller = app.Client();
        using var firstBuyer = app.Client();
        using var secondBuyer = app.Client();
        await app.Register(seller);
        await app.Register(firstBuyer);
        await app.Register(secondBuyer);
        var listingId = await app.Artwork(seller, localPickupAvailable: true);

        var responses = await Task.WhenAll(PlaceOrder(firstBuyer, listingId), PlaceOrder(secondBuyer, listingId));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        var sales = await Json(await seller.GetAsync("/api/orders?role=seller"));
        Assert.Single(sales.EnumerateArray());
    }

    [Fact]
    public async Task Accepting_two_offers_at_the_same_time_creates_only_one_order()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var seller = app.Client();
        using var firstBuyer = app.Client();
        using var secondBuyer = app.Client();
        await app.Register(seller);
        await app.Register(firstBuyer);
        await app.Register(secondBuyer);
        var listingId = await app.Artwork(seller, localPickupAvailable: true);
        var firstOfferId = await MakeOffer(firstBuyer, listingId, 6);
        var secondOfferId = await MakeOffer(secondBuyer, listingId, 7);

        var responses = await Task.WhenAll(
            seller.PostAsync($"/api/offers/{firstOfferId}/accept", null),
            seller.PostAsync($"/api/offers/{secondOfferId}/accept", null));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        var sales = await Json(await seller.GetAsync("/api/orders?role=seller"));
        Assert.Single(sales.EnumerateArray());
        Assert.Equal("Reserved", await ListingStatus(seller, listingId));
    }
}