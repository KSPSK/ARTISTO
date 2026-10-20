using ARTISTO.Models;
using Microsoft.EntityFrameworkCore;

namespace ARTISTO.Data;

public class UserRepository : IUserRepository
{
    private readonly ArtistoDbContext _context;

    public UserRepository(ArtistoDbContext context)
    {
        _context = context;
    }

    public async Task<List<User>> GetAllAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.DisplayName)
            .ToListAsync();
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        var normalized = username.Trim().ToLowerInvariant();
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == normalized);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Users.AnyAsync(u => u.Id == id);
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        var normalized = username.Trim().ToLowerInvariant();
        return await _context.Users
            .AnyAsync(u => u.Username.ToLower() == normalized);
    }

    public async Task<List<ArtworkListing>> GetListingsByUserIdAsync(int userId)
    {
        return await _context.ArtworkListings
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .ToListAsync();
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
