using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ARTISTO.Tests;

public class ReviewApiTests
{
    private static async Task<int> CreateListing(HttpClient seller)
    {
        var response = await seller.PostAsJsonAsync("/api/artworklistings", new
        {
            title = "Review test artwork",
            description = "Test",
            price = 100,
            creatorName = "Test",
            category = "Painting",
            localPickupAvailable = true
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private static async Task<int> PlaceOrder(HttpClient buyer, int listingId)
    {
        var response = await buyer.PostAsJsonAsync("/api/orders", new { artworkListingId = listingId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private static async Task<int> CompletedOrder(HttpClient seller, HttpClient buyer)
    {
        var listingId = await CreateListing(seller);
        var orderId = await PlaceOrder(buyer, listingId);
        Assert.Equal(HttpStatusCode.OK, (await seller.PostAsync($"/api/orders/{orderId}/handover", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await buyer.PostAsync($"/api/orders/{orderId}/complete", null)).StatusCode);
        return orderId;
    }

    private static Task<HttpResponseMessage> LeaveReview(HttpClient client, int orderId, int rating, string? comment = null)
    {
        return client.PostAsJsonAsync("/api/reviews", new { orderId, rating, comment });
    }

    [Fact]
    public async Task Leaving_a_review_requires_authentication()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var anonymous = app.Client();

        var response = await LeaveReview(anonymous, 1, 5);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Buyer_can_review_a_completed_order_only_once()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var seller = app.Client();
        using var buyer = app.Client();
        var sellerId = await app.Register(seller);
        var buyerId = await app.Register(buyer);
        var orderId = await CompletedOrder(seller, buyer);

        var first = await LeaveReview(buyer, orderId, 5, "  Great painting, easy pickup.  ");
        var second = await LeaveReview(buyer, orderId, 4);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var review = await first.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(5, review.GetProperty("rating").GetInt32());
        Assert.Equal("Great painting, easy pickup.", review.GetProperty("comment").GetString());
        Assert.Equal(buyerId, review.GetProperty("reviewerId").GetInt32());
        Assert.Equal(sellerId, review.GetProperty("sellerId").GetInt32());

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Only_the_buyer_can_review_the_order()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var seller = app.Client();
        using var buyer = app.Client();
        using var stranger = app.Client();
        await app.Register(seller);
        await app.Register(buyer);
        await app.Register(stranger);
        var orderId = await CompletedOrder(seller, buyer);

        Assert.Equal(HttpStatusCode.Forbidden, (await LeaveReview(seller, orderId, 5)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await LeaveReview(stranger, orderId, 5)).StatusCode);
    }

    [Fact]
    public async Task Order_must_be_completed_before_it_can_be_reviewed()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var seller = app.Client();
        using var buyer = app.Client();
        await app.Register(seller);
        await app.Register(buyer);
        var orderId = await PlaceOrder(buyer, await CreateListing(seller));

        Assert.Equal(HttpStatusCode.BadRequest, (await LeaveReview(buyer, orderId, 5)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await seller.PostAsync($"/api/orders/{orderId}/handover", null)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await LeaveReview(buyer, orderId, 5)).StatusCode);
    }

    [Fact]
    public async Task Cancelled_order_cannot_be_reviewed()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var seller = app.Client();
        using var buyer = app.Client();
        await app.Register(seller);
        await app.Register(buyer);
        var orderId = await PlaceOrder(buyer, await CreateListing(seller));
        Assert.Equal(HttpStatusCode.OK, (await buyer.PostAsJsonAsync($"/api/orders/{orderId}/cancel", new { reason = "Changed my mind" })).StatusCode);

        var response = await LeaveReview(buyer, orderId, 3);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task Rating_must_be_between_one_and_five(int rating)
    {
        using var app = new SocialApp();
        await app.Seed();
        using var seller = app.Client();
        using var buyer = app.Client();
        await app.Register(seller);
        await app.Register(buyer);
        var orderId = await CompletedOrder(seller, buyer);

        var response = await LeaveReview(buyer, orderId, rating);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Missing_order_returns_not_found()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var buyer = app.Client();
        await app.Register(buyer);

        var response = await LeaveReview(buyer, 999999, 5);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Seller_reviews_are_public_and_show_the_average_rating()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var seller = app.Client();
        using var firstBuyer = app.Client();
        using var secondBuyer = app.Client();
        using var anonymous = app.Client();
        var sellerId = await app.Register(seller);
        await app.Register(firstBuyer);
        await app.Register(secondBuyer);

        Assert.Equal(HttpStatusCode.Created, (await LeaveReview(firstBuyer, await CompletedOrder(seller, firstBuyer), 5)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await LeaveReview(secondBuyer, await CompletedOrder(seller, secondBuyer), 4)).StatusCode);

        var response = await anonymous.GetAsync($"/api/users/{sellerId}/reviews");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, summary.GetProperty("reviewCount").GetInt32());
        Assert.Equal(4.5, summary.GetProperty("averageRating").GetDouble());
        Assert.Equal(2, summary.GetProperty("reviews").GetArrayLength());
    }

    [Fact]
    public async Task Seller_without_reviews_has_no_average()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var seller = app.Client();
        var sellerId = await app.Register(seller);

        var summary = await (await seller.GetAsync($"/api/users/{sellerId}/reviews")).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(0, summary.GetProperty("reviewCount").GetInt32());
        Assert.Equal(JsonValueKind.Null, summary.GetProperty("averageRating").ValueKind);
    }
}