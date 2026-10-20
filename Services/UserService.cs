using ARTISTO.Data;
using ARTISTO.Models;
using Microsoft.AspNetCore.Identity;

namespace ARTISTO.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserService(
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<List<UserResponse>> GetAllAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.Select(ToResponse).ToList();
    }

    public async Task<UserResponse?> GetByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        return user is null ? null : ToResponse(user);
    }

    public async Task<ServiceResult<List<ArtworkListing>>> GetListingsAsync(int id)
    {
        var exists = await _userRepository.ExistsAsync(id);
        if (!exists)
        {
            return ServiceResult<List<ArtworkListing>>.NotFound($"User {id} was not found.");
        }

        var listings = await _userRepository.GetListingsByUserIdAsync(id);
        return ServiceResult<List<ArtworkListing>>.Success(listings);
    }

    public async Task<ServiceResult<User>> RegisterAsync(UserRegisterRequest request)
    {
        var exists = await _userRepository.UsernameExistsAsync(request.Username);
        if (exists)
        {
            return ServiceResult<User>.BadRequest($"Username '{request.Username}' is already taken.");
        }

        var user = new User
        {
            Username = request.Username.Trim(),
            DisplayName = request.DisplayName.Trim(),
            Description = request.Description.Trim(),
            City = request.City.Trim()
        };

        user.Password = _passwordHasher.HashPassword(user, request.Password);

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        return ServiceResult<User>.Success(user);
    }

    public async Task<ServiceResult<User>> AuthenticateAsync(UserLoginRequest request)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username);
        if (user is null)
        {
            return ServiceResult<User>.Unauthorized("Invalid username or password.");
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.Password, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return ServiceResult<User>.Unauthorized("Invalid username or password.");
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.Password = _passwordHasher.HashPassword(user, request.Password);
            await _userRepository.SaveChangesAsync();
        }

        return ServiceResult<User>.Success(user);
    }

    public async Task<ServiceResult<UserResponse>> UpdateAsync(
        int id,
        UserUpdateRequest request,
        int currentUserId)
    {
        if (currentUserId != id)
        {
            return ServiceResult<UserResponse>.Forbidden();
        }

        var existing = await _userRepository.GetByIdAsync(id);
        if (existing is null)
        {
            return ServiceResult<UserResponse>.NotFound($"User {id} was not found.");
        }

        existing.DisplayName = request.DisplayName.Trim();
        existing.Description = request.Description.Trim();
        existing.City = request.City.Trim();

        await _userRepository.SaveChangesAsync();

        return ServiceResult<UserResponse>.Success(ToResponse(existing));
    }

    public static UserResponse ToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Description = user.Description,
            City = user.City
        };
    }
}
