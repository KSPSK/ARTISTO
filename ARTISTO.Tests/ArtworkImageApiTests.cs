using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ARTISTO.Data;
using ARTISTO.Models;
using ARTISTO.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace ARTISTO.Tests;

public sealed class ArtworkImageApiTests
{
    [Fact]
    public async Task Upload_and_replace_serve_the_current_database_image_and_removal_clears_the_listing()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var owner = app.Client();
        using var visitor = app.Client();
        await app.Register(owner);
        var listing = $"/api/artworklistings/{await app.Artwork(owner)}";
        var imagePath = $"{listing}/image";

        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync(imagePath)).StatusCode);

        var png = CreateImage(jpeg: false);
        var firstUrl = await UploadAsync(owner, imagePath, png);
        Assert.StartsWith($"{imagePath}?v=", firstUrl, StringComparison.Ordinal);
        await AssertImageAsync(visitor, firstUrl, "image/png", png);
        var firstListing = await visitor.GetFromJsonAsync<ArtworkResponse>(listing);
        Assert.Equal(firstUrl, firstListing!.ImageUrl);
        Assert.True(firstListing.HasImage);

        var jpeg = CreateImage(jpeg: true);
        var secondUrl = await UploadAsync(owner, imagePath, jpeg);
        Assert.NotEqual(firstUrl, secondUrl);
        await AssertImageAsync(visitor, secondUrl, "image/jpeg", jpeg);
        await AssertImageAsync(visitor, imagePath, "image/jpeg", jpeg);

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ArtistoDbContext>();
            var stored = await db.ArtworkImages.SingleAsync();
            Assert.Equal("image/jpeg", stored.ContentType);
            Assert.Equal(jpeg, stored.Content);
        }

        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync(imagePath)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync(secondUrl)).StatusCode);
        var updated = await visitor.GetFromJsonAsync<ArtworkResponse>(listing);
        Assert.Null(updated!.ImageUrl);
        Assert.False(updated.HasImage);

        var thirdUrl = await UploadAsync(owner, imagePath, png);
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync(listing)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync(thirdUrl)).StatusCode);
        using var verify = app.Services.CreateScope();
        Assert.False(await verify.ServiceProvider.GetRequiredService<ArtistoDbContext>().ArtworkImages.AnyAsync());
    }

    [Fact]
    public async Task Invalid_uploads_and_non_owner_changes_preserve_the_existing_image()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var owner = app.Client();
        using var other = app.Client();
        using var visitor = app.Client();
        await app.Register(owner);
        await app.Register(other);
        var imagePath = $"/api/artworklistings/{await app.Artwork(owner)}/image";
        var png = CreateImage(jpeg: false);
        var url = await UploadAsync(owner, imagePath, png);

        using var invalid = ImageUpload([0, 1, 2]);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsync(imagePath, invalid)).StatusCode);
        using var oversized = ImageUpload(new byte[5 * 1024 * 1024 + 1]);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsync(imagePath, oversized)).StatusCode);
        using var replacement = ImageUpload(CreateImage(jpeg: true));
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PostAsync(imagePath, replacement)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync(imagePath)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.DeleteAsync(imagePath)).StatusCode);
        await AssertImageAsync(visitor, url, "image/png", png);
    }

    [Theory]
    [InlineData(ServiceStatus.NotFound)]
    [InlineData(ServiceStatus.Forbidden)]
    [InlineData(ServiceStatus.Success)]
    public async Task Upload_allows_listing_changes_during_validation_and_rechecks_before_save(ServiceStatus expectedStatus)
    {
        using var app = new SocialApp();
        await app.Seed();
        using var owner = app.Client();
        var ownerId = await app.Register(owner);
        var listingId = await app.Artwork(owner);

        using var uploadScope = app.Services.CreateScope();
        var db = uploadScope.ServiceProvider.GetRequiredService<ArtistoDbContext>();
        var imageService = new CallbackImageService(async () =>
        {
            Assert.Null(db.Database.CurrentTransaction);

            using var changeScope = app.Services.CreateScope();
            var changeDb = changeScope.ServiceProvider.GetRequiredService<ArtistoDbContext>();
            var listing = await changeDb.ArtworkListings.SingleAsync(x => x.Id == listingId);

            switch (expectedStatus)
            {
                case ServiceStatus.NotFound:
                    changeDb.ArtworkListings.Remove(listing);
                    break;
                case ServiceStatus.Forbidden:
                    listing.UserId = null;
                    break;
                case ServiceStatus.Success:
                    listing.Status = ArtworkListingStatus.Reserved;
                    break;
            }

            await changeDb.SaveChangesAsync();
        });

        var service = new ArtworkListingService(new ArtworkListingRepository(db), imageService, db);
        using var content = new MemoryStream(CreateImage(jpeg: false));
        var image = new FormFile(content, 0, content.Length, "image", "artwork.png");
        var result = await service.UploadImageAsync(listingId, image, ownerId);

        Assert.Equal(expectedStatus, result.Status);
        using var verifyScope = app.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ArtistoDbContext>();
        var stored = await verifyDb.ArtworkImages.SingleOrDefaultAsync(x => x.ArtworkListingId == listingId);
        if (expectedStatus == ServiceStatus.Success)
        {
            Assert.Equal(ArtworkListingStatus.Reserved, result.Value!.Status);
            Assert.NotNull(stored);
            Assert.Equal(content.ToArray(), stored.Content);
        }
        else
        {
            Assert.Null(stored);
        }
    }

    private sealed class CallbackImageService(Func<Task> onRead) : IArtworkImageService
    {
        public async Task<ArtworkImage> ReadAsync(int listingId, IFormFile image, CancellationToken cancellationToken = default)
        {
            await onRead();
            return await new ArtworkImageService().ReadAsync(listingId, image, cancellationToken);
        }
    }

    private static async Task<string> UploadAsync(HttpClient client, string path, byte[] content)
    {
        using var upload = ImageUpload(content);
        using var response = await client.PostAsync(path, upload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var listing = await response.Content.ReadFromJsonAsync<ArtworkResponse>();
        return Assert.IsType<string>(listing!.ImageUrl);
    }

    private static async Task AssertImageAsync(HttpClient client, string url, string mediaType, byte[] content)
    {
        using var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(mediaType, response.Content.Headers.ContentType!.MediaType);
        Assert.Equal(content, await response.Content.ReadAsByteArrayAsync());
    }

    private static MultipartFormDataContent ImageUpload(byte[] content)
    {
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        var upload = new MultipartFormDataContent();
        upload.Add(file, "image", "artwork.bin");
        return upload;
    }

    private static byte[] CreateImage(bool jpeg)
    {
        using var image = new Image<Rgba32>(1, 1, new Rgba32(10, 20, 30, 255));
        using var stream = new MemoryStream();
        if (jpeg)
        {
            image.SaveAsJpeg(stream);
        }
        else
        {
            image.SaveAsPng(stream);
        }

        return stream.ToArray();
    }
}
