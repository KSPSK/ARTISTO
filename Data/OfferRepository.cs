using ARTISTO.Models;
using Microsoft.EntityFrameworkCore;

namespace ARTISTO.Data;

public class OfferRepository(ArtistoDbContext db) : IOfferRepository
{
    public Task<bool> BuyerExistsAsync(int buyerId, CancellationToken cancellationToken)
    {
        return db.Users.AnyAsync(user => user.Id == buyerId, cancellationToken);
    }

    public Task<ArtworkListing?> GetListingAsync(int listingId, CancellationToken cancellationToken)
    {
        return db.ArtworkListings.AsNoTracking().SingleOrDefaultAsync(listing => listing.Id == listingId, cancellationToken);
    }

    public async Task<int?> TryInsertAsync(Offer offer, CancellationToken cancellationToken)
    {
        // Check eligibility in the insert as well, in case the listing changed since validation.
        // The conflict target matches the filtered index for pending offers (Status = 0).
        var ids = await db.Database.SqlQuery<int>($"""
            INSERT INTO "Offers" ("ArtworkListingId", "BuyerId", "Amount", "Message", "Status", "CreatedAtUtc")
            SELECT listing."Id", buyer."Id", {offer.Amount}, NULLIF({offer.Message ?? string.Empty}, ''),
                {(int)offer.Status}, {offer.CreatedAtUtc}
            FROM "ArtworkListings" AS listing
            JOIN "Users" AS buyer ON buyer."Id" = {offer.BuyerId}
            WHERE listing."Id" = {offer.ArtworkListingId}
                AND listing."Status" = {(int)ArtworkListingStatus.Available}
                AND listing."LocalPickupAvailable" = TRUE
                AND listing."UserId" <> buyer."Id"
            ON CONFLICT ("BuyerId", "ArtworkListingId") WHERE "Status" = 0 DO NOTHING
            RETURNING "Id"
            """).ToListAsync(cancellationToken);

        return ids.Count == 0 ? null : ids.Single();
    }

    public async Task<List<Offer>> GetOffersForUserAsync(int userId, string? role)
    {
        var query = db.Offers
            .AsNoTracking()
            .Include(offer => offer.ArtworkListing)
                .ThenInclude(listing => listing!.User)
            .Include(offer => offer.Buyer)
            .AsQueryable();

        if (string.Equals(role, "buyer", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(offer => offer.BuyerId == userId);
        }
        else if (string.Equals(role, "seller", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(offer => offer.ArtworkListing!.UserId == userId);
        }
        else
        {
            query = query.Where(offer => offer.BuyerId == userId || offer.ArtworkListing!.UserId == userId);
        }

        return await query
            .OrderByDescending(offer => offer.CreatedAtUtc)
            .ThenByDescending(offer => offer.Id)
            .ToListAsync();
    }

    public async Task<Offer?> GetByIdAsync(int id)
    {
        return await db.Offers
            .AsNoTracking()
            .Include(offer => offer.ArtworkListing)
                .ThenInclude(listing => listing!.User)
            .Include(offer => offer.Buyer)
            .FirstOrDefaultAsync(offer => offer.Id == id);
    }

    public async Task<Offer?> GetByIdForUpdateAsync(int id)
    {
        return await db.Offers
            .Include(offer => offer.ArtworkListing)
                .ThenInclude(listing => listing!.User)
            .Include(offer => offer.Buyer)
            .FirstOrDefaultAsync(offer => offer.Id == id);
    }

    public async Task<List<Offer>> GetOpenOffersForListingAsync(int listingId, int exceptOfferId)
    {
        return await db.Offers
            .Where(offer => offer.ArtworkListingId == listingId
                && offer.Id != exceptOfferId
                && (offer.Status == OfferStatus.Pending || offer.Status == OfferStatus.Countered))
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await db.SaveChangesAsync();
    }
}
