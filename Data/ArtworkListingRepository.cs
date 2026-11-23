using ARTISTO.Extensions;
using ARTISTO.Models;
using Microsoft.EntityFrameworkCore;

namespace ARTISTO.Data;

public class ArtworkListingRepository : IArtworkListingRepository
{
    private readonly ArtistoDbContext _context;

    public ArtworkListingRepository(ArtistoDbContext context)
    {
        _context = context;
    }

    public async Task<List<ArtworkListing>> GetAllAsync(
        string? search = null,
        ArtworkCategory? category = null,
        decimal? maxPrice = null)
    {
        var query = _context.ArtworkListings
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmed = search.Trim();
            query = query.Where(item =>
                (item.Title != null && EF.Functions.ILike(item.Title, $"%{trimmed}%")) ||
                (item.CreatorName != null && EF.Functions.ILike(item.CreatorName, $"%{trimmed}%")));
        }

        query = query.Filter(
            category: category,
            maxPrice: maxPrice);

        return await query.ToListAsync();
    }

    public async Task<ArtworkListing?> GetByIdAsync(int id)
    {
        return await _context.ArtworkListings.FindAsync(id);
    }

    public async Task<bool> HasOrdersAsync(int id)
    {
        return await _context.Orders.AnyAsync(o => o.ArtworkListingId == id);
    }

    public async Task<bool> UserExistsAsync(int userId)
    {
        return await _context.Users.AnyAsync(u => u.Id == userId);
    }

    public async Task AddAsync(ArtworkListing listing)
    {
        await _context.ArtworkListings.AddAsync(listing);
    }

    public void Remove(ArtworkListing listing)
    {
        _context.ArtworkListings.Remove(listing);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
