using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ARTISTO.Data;
using ARTISTO.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ARTISTO.Tests;

public class OfferApiTests
{
    private static Task<HttpResponseMessage> SubmitOffer(HttpClient client, int listingId, decimal amount = 75.50m, string? message = null)
    {
        return client.PostAsJsonAsync("/api/offers", new { artworkListingId = listingId, amount, message });
    }

    [Fact]
    public async Task Signed_in_buyer_can_submit_an_offer_without_reserving_the_artwork()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var seller = app.Client();
        using var buyer = app.Client();
        var sellerId = await app.Register(seller);
        var buyerId = await app.Register(buyer);
        var listingId = await app.Artwork(seller, localPickupAvailable: true);

        using var response = await buyer.PostAsJsonAsync("/api/offers", new
        {
            artworkListingId = listingId,
            buyerId = sellerId,
            amount = 7.50m,
            message = "  Would you consider this?  "
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OfferResponse>();
        Assert.NotNull(body);
        Assert.Equal(buyerId, body.BuyerId);
        Assert.Equal(listingId, body.ArtworkListingId);
        Assert.Equal(7.50m, body.Amount);
        Assert.Equal("Would you consider this?", body.Message);
        Assert.Equal(OfferStatus.Pending, body.Status);

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ArtistoDbContext>();
        var saved = await db.Offers.Include(offer => offer.ArtworkListing).SingleAsync();
        Assert.Equal(body.Id, saved.Id);
        Assert.Equal(buyerId, saved.BuyerId);
        Assert.Equal(7.50m, saved.Amount);
        Assert.Equal(ArtworkListingStatus.Available, saved.ArtworkListing!.Status);
        Assert.False(await db.Orders.AnyAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Submitting_requires_a_current_buyer_account(bool deletedAccount)
    {
        using var app = new SocialApp();
        await app.Seed();
        using var client = app.Client();
        if (deletedAccount)
        {
            var userId = await app.Register(client);
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ArtistoDbContext>();
            await db.Users.Where(user => user.Id == userId).ExecuteDeleteAsync();
        }

        using var response = await SubmitOffer(client, 1);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1000000010, 0)]
    [InlineData(1001, 0)]
    [InlineData(1000, 1001)]
    public async Task Invalid_amount_or_message_returns_bad_request(int amountInThousandths, int messageLength)
    {
        using var app = new SocialApp();
        await app.Seed();
        using var buyer = app.Client();
        await app.Register(buyer);

        using var response = await SubmitOffer(buyer, 1, amountInThousandths / 1000m, new string('a', messageLength));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(ArtworkListingStatus.Available, true, true, HttpStatusCode.BadRequest)]
    [InlineData(ArtworkListingStatus.Available, false, false, HttpStatusCode.BadRequest)]
    [InlineData(ArtworkListingStatus.Reserved, false, true, HttpStatusCode.Conflict)]
    [InlineData(ArtworkListingStatus.Sold, false, true, HttpStatusCode.Conflict)]
    public async Task Ineligible_listing_is_rejected(
        ArtworkListingStatus status, bool sellerless, bool localPickupAvailable, HttpStatusCode expected)
    {
        using var app = new SocialApp();
        await app.Seed();
        using var seller = app.Client();
        using var buyer = app.Client();
        var sellerId = await app.Register(seller);
        await app.Register(buyer);
        var listingId = await app.Artwork(seller, localPickupAvailable);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ArtistoDbContext>();
        var listing = await db.ArtworkListings.SingleAsync(artwork => artwork.Id == listingId);
        listing.Status = status;
        listing.UserId = sellerless ? null : sellerId;
        await db.SaveChangesAsync();

        using var response = await SubmitOffer(buyer, listingId);

        Assert.Equal(expected, response.StatusCode);
        Assert.False(await db.Offers.AnyAsync());
    }

    [Fact]
    public async Task Self_offers_and_missing_listings_are_rejected()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var seller = app.Client();
        await app.Register(seller);
        var listingId = await app.Artwork(seller, localPickupAvailable: true);

        using var self = await SubmitOffer(seller, listingId);
        using var missing = await SubmitOffer(seller, int.MaxValue);

        Assert.Equal(HttpStatusCode.Forbidden, self.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Simultaneous_offers_from_the_same_buyer_create_only_one_pending_offer()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var seller = app.Client();
        using var buyer = app.Client();
        await app.Register(seller);
        await app.Register(buyer);
        var listingId = await app.Artwork(seller, localPickupAvailable: true);

        var responses = await Task.WhenAll(SubmitOffer(buyer, listingId), SubmitOffer(buyer, listingId));
        using var created = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        using var conflict = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);

        var error = await conflict.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Contains("pending offer", error!.Message);
        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Null, body.GetProperty("message").ValueKind);

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ArtistoDbContext>();
        Assert.Equal(1, await db.Offers.CountAsync());
    }
}
