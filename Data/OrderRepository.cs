using ARTISTO.Models;
using Microsoft.EntityFrameworkCore;

namespace ARTISTO.Data;

public class OrderRepository : IOrderRepository
{
    private readonly ArtistoDbContext _context;

    public OrderRepository(ArtistoDbContext context)
    {
        _context = context;
    }

    public async Task<List<Order>> GetOrdersForUserAsync(int userId, string? role = null)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.ArtworkListing)
            .Include(o => o.Buyer)
            .Include(o => o.Seller)
            .AsQueryable();

        if (string.Equals(role, "buyer", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(o => o.BuyerId == userId);
        }
        else if (string.Equals(role, "seller", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(o => o.SellerId == userId);
        }
        else
        {
            query = query.Where(o => o.BuyerId == userId || o.SellerId == userId);
        }

        return await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .ThenByDescending(o => o.Id)
            .ToListAsync();
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.ArtworkListing)
            .Include(o => o.Buyer)
            .Include(o => o.Seller)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<ArtworkListing?> GetListingWithUserAsync(int listingId)
    {
        return await _context.ArtworkListings
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == listingId);
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users.FindAsync(userId);
    }

    public async Task<bool> UserExistsAsync(int userId)
    {
        return await _context.Users.AnyAsync(u => u.Id == userId);
    }

    public async Task AddAsync(Order order)
    {
        await _context.Orders.AddAsync(order);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
