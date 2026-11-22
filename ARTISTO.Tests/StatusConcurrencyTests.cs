using ARTISTO.Data;
using ARTISTO.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ARTISTO.Tests;

// Two DbContexts load the same row, like two requests at the same time.
// The second save must fail instead of silently overwriting the first one.
public sealed class StatusConcurrencyTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly int _orderId;
    private readonly int _offerId;

    public StatusConcurrencyTests()
    {
        _connection.Open();
        using var db = NewContext();
        db.Database.EnsureCreated();

        var seller = new User { Username = "seller", Password = "test", DisplayName = "Seller", City = "Vilnius" };
        var buyer = new User { Username = "buyer", Password = "test", DisplayName = "Buyer", City = "Vilnius" };
        var listing = new ArtworkListing { Title = "Artwork", CreatorName = "Artist", Price = 100, User = seller };
        var order = new Order { Buyer = buyer, Seller = seller, ArtworkListing = listing, Price = 100 };
        var offer = new Offer { Buyer = buyer, ArtworkListing = listing, Amount = 60 };
        db.AddRange(order, offer);
        db.SaveChanges();

        _orderId = order.Id;
        _offerId = offer.Id;
    }

    private ArtistoDbContext NewContext()
    {
        return new ArtistoDbContext(new DbContextOptionsBuilder<ArtistoDbContext>().UseSqlite(_connection).Options);
    }

    [Fact]
    public async Task Second_change_to_the_same_order_status_is_rejected()
    {
        await using var first = NewContext();
        await using var second = NewContext();
        var firstCopy = await first.Orders.SingleAsync(order => order.Id == _orderId);
        var secondCopy = await second.Orders.SingleAsync(order => order.Id == _orderId);

        firstCopy.Status = OrderStatus.Cancelled;
        await first.SaveChangesAsync();

        secondCopy.Status = OrderStatus.HandedOver;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        await using var check = NewContext();
        Assert.Equal(OrderStatus.Cancelled, (await check.Orders.SingleAsync(order => order.Id == _orderId)).Status);
    }

    [Fact]
    public async Task Second_change_to_the_same_offer_status_is_rejected()
    {
        await using var first = NewContext();
        await using var second = NewContext();
        var firstCopy = await first.Offers.SingleAsync(offer => offer.Id == _offerId);
        var secondCopy = await second.Offers.SingleAsync(offer => offer.Id == _offerId);

        firstCopy.Status = OfferStatus.Accepted;
        await first.SaveChangesAsync();

        secondCopy.Status = OfferStatus.Declined;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        await using var check = NewContext();
        Assert.Equal(OfferStatus.Accepted, (await check.Offers.SingleAsync(offer => offer.Id == _offerId)).Status);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}