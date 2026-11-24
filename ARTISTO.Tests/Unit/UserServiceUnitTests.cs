using ARTISTO.Data;
using ARTISTO.Models;
using ARTISTO.Services;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace ARTISTO.Tests;

public class UserServiceUnitTests
{
    [Fact]
    public async Task RegisterUser_WhenUsernameIsTaken_ReturnsBadRequest()
    {
        var repository = new FakeUserRepository { UsernameAlreadyExists = true };
        var service = new UserService(repository, new FakePasswordHasher());
        var request = new UserRegisterRequest
        {
            Username = "artist1",
            Password = "password123",
            DisplayName = "Artist One",
            City = "Vilnius"
        };

        var result = await service.RegisterAsync(request);

        Assert.Equal(ServiceStatus.BadRequest, result.Status);
        Assert.Null(repository.AddedUser);
    }

    [Fact]
    public async Task RegisterUser_WithValidInput_TrimsFieldsAndHashesPassword()
    {
        var repository = new FakeUserRepository();
        var service = new UserService(repository, new FakePasswordHasher());
        var request = new UserRegisterRequest
        {
            Username = "  alice  ",
            Password = "secret-password",
            DisplayName = "  Alice Art  ",
            Description = "  Oil painter  ",
            City = "  Vilnius  "
        };

        var result = await service.RegisterAsync(request);

        Assert.Equal(ServiceStatus.Success, result.Status);
        Assert.NotNull(repository.AddedUser);
        Assert.Equal("alice", repository.AddedUser!.Username);
        Assert.Equal("Alice Art", repository.AddedUser.DisplayName);
        Assert.Equal("Oil painter", repository.AddedUser.Description);
        Assert.Equal("Vilnius", repository.AddedUser.City);
        Assert.Equal("HASHED:secret-password", repository.AddedUser.Password);
    }

    [Fact]
    public async Task UpdateUser_WhenCurrentUserDiffersFromTarget_ReturnsForbidden()
    {
        var repository = new FakeUserRepository
        {
            ExistingUser = new User { Id = 1, Username = "alice", DisplayName = "Alice", City = "Vilnius" }
        };
        var service = new UserService(repository, new FakePasswordHasher());
        var request = new UserUpdateRequest { DisplayName = "New Name", City = "Kaunas" };

        var result = await service.UpdateAsync(id: 1, request, currentUserId: 2);

        Assert.Equal(ServiceStatus.Forbidden, result.Status);
        Assert.False(repository.SavedChanges);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public User? ExistingUser { get; init; }
        public bool UsernameAlreadyExists { get; init; }
        public User? AddedUser { get; private set; }
        public bool SavedChanges { get; private set; }

        public Task<List<User>> GetAllAsync() =>
            Task.FromResult(new List<User>());

        public Task<User?> GetByIdAsync(int id) =>
            Task.FromResult(ExistingUser?.Id == id ? ExistingUser : null);

        public Task<User?> GetByUsernameAsync(string username) =>
            Task.FromResult(ExistingUser?.Username == username ? ExistingUser : null);

        public Task<bool> ExistsAsync(int id) =>
            Task.FromResult(ExistingUser?.Id == id);

        public Task<bool> UsernameExistsAsync(string username) =>
            Task.FromResult(UsernameAlreadyExists);

        public Task<List<ArtworkListing>> GetListingsByUserIdAsync(int userId) =>
            Task.FromResult(new List<ArtworkListing>());

        public Task AddAsync(User user)
        {
            AddedUser = user;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync()
        {
            SavedChanges = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher<User>
    {
        public string HashPassword(User user, string password) =>
            $"HASHED:{password}";

        public PasswordVerificationResult VerifyHashedPassword(
            User user,
            string hashedPassword,
            string providedPassword) =>
            hashedPassword == $"HASHED:{providedPassword}"
                ? PasswordVerificationResult.Success
                : PasswordVerificationResult.Failed;
    }
}
