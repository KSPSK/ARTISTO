using ARTISTO.Models;

namespace ARTISTO.Data;

public interface IOrderRepository
{
    Task<List<Order>> GetOrdersForUserAsync(int userId, string? role = null);
    Task<Order?> GetByIdAsync(int id);
    Task<ArtworkListing?> GetListingWithUserAsync(int listingId);
    Task<User?> GetUserByIdAsync(int userId);
    Task<bool> UserExistsAsync(int userId);
    Task AddAsync(Order order);
    Task SaveChangesAsync();
}
