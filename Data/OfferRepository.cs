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
                AND listing."UserId" <> buyer."Id"
            ON CONFLICT ("BuyerId", "ArtworkListingId") WHERE "Status" = 0 DO NOTHING
            RETURNING "Id"
            """).ToListAsync(cancellationToken);

        return ids.Count == 0 ? null : ids.Single();
    }
}
