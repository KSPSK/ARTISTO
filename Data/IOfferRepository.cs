using ARTISTO.Models;

namespace ARTISTO.Data;

public interface IOfferRepository
{
    Task<bool> BuyerExistsAsync(int buyerId, CancellationToken cancellationToken);
    Task<ArtworkListing?> GetListingAsync(int listingId, CancellationToken cancellationToken);
    Task<int?> TryInsertAsync(Offer offer, CancellationToken cancellationToken);
}
