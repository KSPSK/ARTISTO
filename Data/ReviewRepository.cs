using ARTISTO.Models;
using Microsoft.EntityFrameworkCore;

namespace ARTISTO.Data;

public class ReviewRepository : IReviewRepository
{
    private readonly ArtistoDbContext _context;

    public ReviewRepository(ArtistoDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetOrderWithListingAsync(int orderId)
    {
        return await _context.Orders
            .Include(o => o.ArtworkListing)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users.FindAsync(userId);
    }

    public async Task<bool> UserExistsAsync(int userId)
    {
        return await _context.Users.AnyAsync(u => u.Id == userId);
    }

    public async Task<bool> ReviewExistsForOrderAsync(int orderId)
    {
        return await _context.Reviews.AnyAsync(r => r.OrderId == orderId);
    }

    public async Task<Review?> GetByIdAsync(int id)
    {
        return await _context.Reviews
            .AsNoTracking()
            .Include(r => r.ArtworkListing)
            .Include(r => r.Reviewer)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<Review>> GetForSellerAsync(int sellerId)
    {
        return await _context.Reviews
            .AsNoTracking()
            .Include(r => r.ArtworkListing)
            .Include(r => r.Reviewer)
            .Where(r => r.SellerId == sellerId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ThenByDescending(r => r.Id)
            .ToListAsync();
    }

    public async Task AddAsync(Review review)
    {
        await _context.Reviews.AddAsync(review);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}