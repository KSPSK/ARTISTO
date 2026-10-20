using ARTISTO.Models;

namespace ARTISTO.Data;

public interface IUserRepository
{
    Task<List<User>> GetAllAsync();
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUsernameAsync(string username);
    Task<bool> ExistsAsync(int id);
    Task<bool> UsernameExistsAsync(string username);
    Task<List<ArtworkListing>> GetListingsByUserIdAsync(int userId);
    Task AddAsync(User user);
    Task SaveChangesAsync();
}
