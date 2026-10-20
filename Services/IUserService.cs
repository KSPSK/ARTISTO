using ARTISTO.Models;

namespace ARTISTO.Services;

public interface IUserService
{
    Task<List<UserResponse>> GetAllAsync();
    Task<UserResponse?> GetByIdAsync(int id);
    Task<ServiceResult<List<ArtworkListing>>> GetListingsAsync(int id);
    Task<ServiceResult<User>> RegisterAsync(UserRegisterRequest request);
    Task<ServiceResult<User>> AuthenticateAsync(UserLoginRequest request);
    Task<ServiceResult<UserResponse>> UpdateAsync(int id, UserUpdateRequest request, int currentUserId);
}
