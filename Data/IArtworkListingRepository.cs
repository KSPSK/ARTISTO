using ARTISTO.Models;

namespace ARTISTO.Data;

public interface IArtworkListingRepository
{
    Task<List<ArtworkListing>> GetAllAsync(string? search = null);
    Task<ArtworkListing?> GetByIdAsync(int id);
    Task<bool> HasOrdersAsync(int id);
    Task<bool> UserExistsAsync(int userId);
    Task AddAsync(ArtworkListing listing);
    void Remove(ArtworkListing listing);
    Task SaveChangesAsync();
}
