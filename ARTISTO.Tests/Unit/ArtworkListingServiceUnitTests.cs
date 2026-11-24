using ARTISTO.Data;
using ARTISTO.Models;
using ARTISTO.Services;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace ARTISTO.Tests;

public class ArtworkListingServiceUnitTests
{
    [Fact]
    public async Task CreateListing_WhenUserDoesNotExist_ReturnsUnauthorized()
    {
        var repository = new FakeArtworkListingRepository { UserExists = false };
        var service = new ArtworkListingService(repository, new FakeArtworkImageService());
        var request = new ArtworkListingWriteRequest
        {
            Title = "Sunset",
            CreatorName = "Alice",
            Price = 120m,
            Category = ArtworkCategory.Painting
        };

        var result = await service.CreateAsync(request, userId: 42);

        Assert.Equal(ServiceStatus.Unauthorized, result.Status);
        Assert.False(repository.SavedChanges);
    }

    [Fact]
    public async Task UpdateListing_WhenCallerIsNotOwner_ReturnsForbidden()
    {
        var repository = new FakeArtworkListingRepository
        {
            ExistingListing = new ArtworkListing
            {
                Id = 5,
                UserId = 10,
                Title = "Original Title",
                Price = 50m,
                Status = ArtworkListingStatus.Available
            }
        };
        var service = new ArtworkListingService(repository, new FakeArtworkImageService());
        var request = new ArtworkListingWriteRequest
        {
            Title = "Updated Title",
            CreatorName = "Alice",
            Price = 50m
        };

        var result = await service.UpdateAsync(id: 5, request, userId: 99);

        Assert.Equal(ServiceStatus.Forbidden, result.Status);
        Assert.False(repository.SavedChanges);
    }

    [Fact]
    public async Task UpdateListing_ChangingPriceWhileReserved_ReturnsConflict()
    {
        var repository = new FakeArtworkListingRepository
        {
            ExistingListing = new ArtworkListing
            {
                Id = 5,
                UserId = 10,
                Title = "Reserved Painting",
                Price = 50m,
                Status = ArtworkListingStatus.Reserved
            }
        };
        var service = new ArtworkListingService(repository, new FakeArtworkImageService());
        var request = new ArtworkListingWriteRequest
        {
            Title = "Reserved Painting",
            CreatorName = "Alice",
            Price = 80m
        };

        var result = await service.UpdateAsync(id: 5, request, userId: 10);

        Assert.Equal(ServiceStatus.Conflict, result.Status);
        Assert.False(repository.SavedChanges);
    }

    [Fact]
    public async Task DeleteListing_WhenListingHasOrders_ReturnsBadRequest()
    {
        var repository = new FakeArtworkListingRepository
        {
            ExistingListing = new ArtworkListing
            {
                Id = 7,
                UserId = 10,
                Title = "Ordered Artwork",
                ImageUrl = "/uploads/artworks/sample.jpg"
            },
            ListingHasOrders = true
        };
        var images = new FakeArtworkImageService();
        var service = new ArtworkListingService(repository, images);

        var result = await service.DeleteAsync(id: 7, userId: 10);

        Assert.Equal(ServiceStatus.BadRequest, result.Status);
        Assert.False(repository.SavedChanges);
        Assert.Null(images.DeletedImageUrl);
    }

    [Fact]
    public async Task UploadImage_WhenValidationExceptionIsThrown_ReturnsBadRequestWithMessage()
    {
        var repository = new FakeArtworkListingRepository
        {
            ExistingListing = new ArtworkListing { Id = 3, UserId = 10, Title = "Portrait" }
        };
        var images = new FakeArtworkImageService
        {
            ExceptionToThrow = new ArtworkImageValidationException("The image must be 5 MB or smaller.")
        };
        var service = new ArtworkListingService(repository, images);
        var file = new FormFile(Stream.Null, 0, 0, "image", "oversized.jpg");

        var result = await service.UploadImageAsync(id: 3, file, userId: 10);

        Assert.Equal(ServiceStatus.BadRequest, result.Status);
        Assert.Equal("The image must be 5 MB or smaller.", result.ErrorMessage);
        Assert.False(repository.SavedChanges);
    }

    private sealed class FakeArtworkListingRepository : IArtworkListingRepository
    {
        public ArtworkListing? ExistingListing { get; init; }
        public bool ListingHasOrders { get; init; }
        public bool UserExists { get; init; } = true;
        public bool SavedChanges { get; private set; }

        public Task<List<ArtworkListing>> GetAllAsync(string? search = null) =>
            Task.FromResult(new List<ArtworkListing>());

        public Task<List<ArtworkListing>> GetAllAsync(
            string? search,
            ArtworkCategory? category,
            decimal? maxPrice) =>
            Task.FromResult(new List<ArtworkListing>());

        public Task<ArtworkListing?> GetByIdAsync(int id) =>
            Task.FromResult(ExistingListing?.Id == id ? ExistingListing : null);

        public Task<bool> HasOrdersAsync(int id) =>
            Task.FromResult(ListingHasOrders);

        public Task<bool> UserExistsAsync(int userId) =>
            Task.FromResult(UserExists);

        public Task AddAsync(ArtworkListing listing) =>
            Task.CompletedTask;

        public void Remove(ArtworkListing listing)
        {
        }

        public Task SaveChangesAsync()
        {
            SavedChanges = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeArtworkImageService : IArtworkImageService
    {
        public Exception? ExceptionToThrow { get; init; }
        public string? DeletedImageUrl { get; private set; }

        public Task<string> SaveAsync(IFormFile image, CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult("/uploads/artworks/test.jpg");
        }

        public void Delete(string? imageUrl)
        {
            DeletedImageUrl = imageUrl;
        }
    }
}
