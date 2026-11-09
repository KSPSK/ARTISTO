using ARTISTO.Models;

namespace ARTISTO.Data;

public interface IReviewRepository
{
    Task<Order?> GetOrderWithListingAsync(int orderId);
    Task<User?> GetUserByIdAsync(int userId);
    Task<bool> UserExistsAsync(int userId);
    Task<bool> ReviewExistsForOrderAsync(int orderId);
    Task<Review?> GetByIdAsync(int id);
    Task<List<Review>> GetForSellerAsync(int sellerId);
    Task AddAsync(Review review);
    Task SaveChangesAsync();
}