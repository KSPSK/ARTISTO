using ARTISTO.Models;

namespace ARTISTO.Data;

public interface IOfferRepository
{
    Task<bool> BuyerExistsAsync(int buyerId, CancellationToken cancellationToken);
    Task<ArtworkListing?> GetListingAsync(int listingId, CancellationToken cancellationToken);
    Task<int?> TryInsertAsync(Offer offer, CancellationToken cancellationToken);
    Task<List<Offer>> GetOffersForUserAsync(int userId, string? role);
    Task<Offer?> GetByIdAsync(int id);
    Task<Offer?> GetByIdForUpdateAsync(int id);
    Task<List<Offer>> GetOpenOffersForListingAsync(int listingId, int exceptOfferId);
    Task<List<Offer>> DeclineOpenOffersAsync(int listingId, int exceptOfferId);
    Task DeclineIfOpenAsync(int offerId);
    Task SaveChangesAsync();
}
