using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ARTISTO.Data;
using ARTISTO.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace ARTISTO.Tests;

// Each fixture owns its SQLite file. No application credentials or shared databases are used.
public sealed class SocialApp : WebApplicationFactory<Program>
{
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"artisto-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ArtistoDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ArtistoDbContext>>();
            services.AddDbContext<ArtistoDbContext>(options => options.UseSqlite($"Data Source={_file};Foreign Keys=True;Pooling=False"));
        });
    }

    public HttpClient Client() => CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    public async Task Seed()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ArtistoDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public async Task<int> Register(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/users/register", new
        {
            username = Guid.NewGuid().ToString("N"),
            password = "test-password",
            displayName = "Test",
            city = "Vilnius"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<UserResponse>())!.Id;
    }

    public async Task<int> Artwork(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/artworklistings", new
        {
            title = "Test artwork",
            description = "Test",
            price = 10,
            creatorName = "Test",
            category = "Painting"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ArtworkResponse>())!.Id;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_file))
        {
            File.Delete(_file);
        }
    }
}

public class SocialTests
{
    [Fact]
    public async Task Deleted_account_cannot_mutate_or_read_private_data_with_a_stale_cookie()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var client = app.Client();
        var id = await app.Register(client);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ArtistoDbContext>();
            await db.Users.Where(x => x.Id == id).ExecuteDeleteAsync();
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me/favorites")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/artworklistings/1/favorite")).StatusCode);
        foreach (var path in new[] { "artworklistings/1/like", "artworklistings/1/favorite" })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsync($"/api/{path}", null)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.DeleteAsync($"/api/{path}")).StatusCode);
        }
    }

    private static async Task<JsonElement> Json(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Theory]
    [InlineData("/api/artworklistings/1/like", "PUT")]
    [InlineData("/api/artworklistings/1/like", "DELETE")]
    [InlineData("/api/artworklistings/1/favorite", "PUT")]
    [InlineData("/api/artworklistings/1/favorite", "DELETE")]
    [InlineData("/api/artworklistings/1/favorite", "GET")]
    [InlineData("/api/users/me/favorites", "GET")]
    public async Task Private_routes_require_authentication(string url, string method)
    {
        using var app = new SocialApp();
        using var client = app.Client();
        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Own_artwork_actions_are_independent_idempotent_and_private()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var owner = app.Client();
        using var other = app.Client();
        using var anonymous = app.Client();
        var ownerId = await app.Register(owner);
        var otherId = await app.Register(other);
        var art = await app.Artwork(owner);
        foreach (var action in new[] { "like", "favorite" })
        {
            for (var i = 0; i < 2; i++)
            {
                // Supplied acting IDs are ignored; only the cookie determines the actor.
                Assert.Equal(HttpStatusCode.NoContent, (await owner.PutAsync($"/api/artworklistings/{art}/{action}?userId={otherId}", null)).StatusCode);
            }
        }
        var likes = await Json(anonymous, $"/api/artworklistings/{art}/likes");
        Assert.Equal(1, likes.GetProperty("likeCount").GetInt32());
        Assert.False(likes.GetProperty("isLiked").GetBoolean());
        Assert.True((await Json(owner, $"/api/artworklistings/{art}/likes")).GetProperty("isLiked").GetBoolean());
        Assert.True((await Json(owner, $"/api/artworklistings/{art}/favorite")).GetProperty("isSaved").GetBoolean());
        Assert.False((await Json(other, $"/api/artworklistings/{art}/favorite")).GetProperty("isSaved").GetBoolean());
        Assert.Equal(0, (await Json(other, "/api/users/me/favorites")).GetProperty("totalCount").GetInt32());
        var saved = await Json(owner, "/api/users/me/favorites");
        Assert.Equal(1, saved.GetProperty("totalCount").GetInt32());
        Assert.DoesNotContain("password", saved.ToString(), StringComparison.OrdinalIgnoreCase);
        var publicListing = await Json(anonymous, $"/api/artworklistings/{art}");
        Assert.Equal(1, publicListing.GetProperty("likeCount").GetInt32());
        Assert.False(publicListing.GetProperty("isSaved").GetBoolean());
        Assert.True((await Json(owner, $"/api/artworklistings/{art}")).GetProperty("isSaved").GetBoolean());
        foreach (var path in new[] { "/api/artworklistings", $"/api/users/{ownerId}/artworklistings" })
        {
            Assert.True((await Json(owner, path))[0].GetProperty("isSaved").GetBoolean());
            Assert.False((await Json(other, path))[0].GetProperty("isSaved").GetBoolean());
            Assert.False((await Json(anonymous, path))[0].GetProperty("isLiked").GetBoolean());
        }
        for (var i = 0; i < 2; i++)
        {
            Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/artworklistings/{art}/like")).StatusCode);
        }
        Assert.True((await Json(owner, $"/api/artworklistings/{art}/favorite")).GetProperty("isSaved").GetBoolean());
        for (var i = 0; i < 2; i++)
        {
            Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/artworklistings/{art}/favorite")).StatusCode);
        }
        Assert.Equal(0, (await Json(owner, "/api/users/me/favorites")).GetProperty("totalCount").GetInt32());
        Assert.Equal(0, (await Json(anonymous, $"/api/artworklistings/{art}/likes")).GetProperty("likeCount").GetInt32());
    }

    [Fact]
    public async Task Missing_targets_and_invalid_pagination_return_consistent_errors()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var client = app.Client();
        var id = await app.Register(client);
        foreach (var path in new[] { "artworklistings/999/like", "artworklistings/999/favorite" })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsync($"/api/{path}", null)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/{path}")).StatusCode);
        }
        foreach (var path in new[] { "artworklistings/999/likes", "artworklistings/999/favorite" })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/{path}")).StatusCode);
        }
        foreach (var query in new[] { "page=0", "pageSize=0", "pageSize=101", "page=2147483647&pageSize=100" })
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/users/me/favorites?{query}")).StatusCode);
        }
    }

    [Fact]
    public async Task Lists_have_bounded_pages_and_deterministic_order()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var owner = app.Client();
        var ownerId = await app.Register(owner);
        var ids = new List<int>();
        for (var i = 0; i < 3; i++)
        {
            ids.Add(await app.Artwork(owner));
        }
        foreach (var id in ids.AsEnumerable().Reverse())
        {
            await owner.PutAsync($"/api/artworklistings/{id}/favorite", null);
        }
        var first = await Json(owner, "/api/users/me/favorites?pageSize=2");
        var second = await Json(owner, "/api/users/me/favorites?pageSize=2&page=2");
        Assert.Equal(3, first.GetProperty("totalCount").GetInt32());
        Assert.Equal(ids.Take(2), first.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetInt32()));
        Assert.Equal(ids[2], second.GetProperty("items")[0].GetProperty("id").GetInt32());
        Assert.Equal(0, (await Json(owner, "/api/users/me/favorites?page=5")).GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Concurrent_adds_create_one_pair_and_constraints_reject_invalid_writes()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var client = app.Client();
        using var target = app.Client();
        var user = await app.Register(client);
        var artwork = await app.Artwork(client);
        foreach (var path in new[] { $"artworklistings/{artwork}/like", $"artworklistings/{artwork}/favorite" })
        {
            var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => client.PutAsync($"/api/{path}", null)));
            Assert.All(responses, response => Assert.Equal(HttpStatusCode.NoContent, response.StatusCode));
        }
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ArtistoDbContext>();
        Assert.Equal(1, await db.ArtworkLikes.CountAsync());
        Assert.Equal(1, await db.ArtworkFavorites.CountAsync());
        await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"ArtworkLikes\" VALUES ({user}, {artwork})"));
        await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"ArtworkFavorites\" VALUES ({user}, {artwork})"));
        // The idempotent repository must still surface unrelated FK/check violations.
        var repository = new SocialRepository(db);
        await Assert.ThrowsAsync<SqliteException>(() => repository.SetAsync(SocialAction.Like, user, 999, true));
        await db.ArtworkListings.Where(x => x.Id == artwork).ExecuteDeleteAsync();
        Assert.Equal(0, await db.ArtworkLikes.CountAsync());
        Assert.Equal(0, await db.ArtworkFavorites.CountAsync());
    }
}
