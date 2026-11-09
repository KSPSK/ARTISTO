using ARTISTO.Data;
using ARTISTO.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ARTISTO.Tests;

public sealed class OfferPersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ArtistoDbContext _db;
    private readonly User _buyer = new() { Username = "buyer", Password = "test", DisplayName = "Buyer", City = "Vilnius" };
    private readonly ArtworkListing _listing = new() { Title = "Artwork", CreatorName = "Artist", Price = 100 };

    public OfferPersistenceTests()
    {
        _connection.Open();
        _db = new ArtistoDbContext(new DbContextOptionsBuilder<ArtistoDbContext>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();
        _db.AddRange(_buyer, _listing);
        _db.SaveChanges();
    }

    private Offer NewOffer(decimal amount = 75.50m)
    {
        return new Offer { BuyerId = _buyer.Id, ArtworkListingId = _listing.Id, Amount = amount, Message = "My offer" };
    }

    [Theory]
    [InlineData("0.01")]
    [InlineData("75.50")]
    [InlineData("1000000")]
    public async Task Save_ValidOffer_PreservesDetailsWithoutReservingArtwork(string amount)
    {
        var offer = NewOffer(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture));
        _db.Offers.Add(offer);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var stored = await _db.Offers.Include(x => x.Buyer).Include(x => x.ArtworkListing).SingleAsync();

        Assert.Equal(offer.Id, stored.Id);
        Assert.Equal(offer.Amount, stored.Amount);
        Assert.Equal("My offer", stored.Message);
        Assert.Equal(OfferStatus.Pending, stored.Status);
        Assert.Equal(offer.CreatedAtUtc, stored.CreatedAtUtc);
        Assert.Equal(_buyer.Id, stored.Buyer!.Id);
        Assert.Equal(ArtworkListingStatus.Available, stored.ArtworkListing!.Status);
    }

    [Fact]
    public async Task Save_SecondPendingOfferForSameBuyerAndArtwork_IsRejected()
    {
        _db.Offers.Add(NewOffer());
        await _db.SaveChangesAsync();
        _db.Offers.Add(NewOffer(80));

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1000000.01")]
    public async Task Save_AmountOutsideAllowedRange_IsRejected(string amount)
    {
        _db.Offers.Add(NewOffer(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Delete_BuyerOrArtwork_RemovesAssociatedOffer(bool deleteBuyer)
    {
        _db.Offers.Add(NewOffer());
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        if (deleteBuyer)
        {
            await _db.Users.Where(x => x.Id == _buyer.Id).ExecuteDeleteAsync();
        }
        else
        {
            await _db.ArtworkListings.Where(x => x.Id == _listing.Id).ExecuteDeleteAsync();
        }

        Assert.Empty(await _db.Offers.ToListAsync());
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
