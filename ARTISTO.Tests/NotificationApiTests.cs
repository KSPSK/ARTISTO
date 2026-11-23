using System.Net;
using System.Net.Http.Json;
using ARTISTO.Data;
using ARTISTO.Models;
using ARTISTO.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ARTISTO.Tests;

public class NotificationApiTests
{
    [Theory]
    [InlineData("GET", "/api/notifications")]
    [InlineData("GET", "/api/notifications/unread-count")]
    [InlineData("PATCH", "/api/notifications/1/read")]
    [InlineData("PATCH", "/api/notifications/read-all")]
    public async Task Notifications_require_sign_in(string method, string path)
    {
        using var app = new SocialApp();
        await app.Seed();
        using var client = app.Client();

        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task List_returns_only_own_notifications_in_newest_first_order()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var reader = app.Client();
        using var otherUser = app.Client();

        var readerId = await app.Register(reader);
        var otherUserId = await app.Register(otherUser);
        var date = new DateTime(2026, 11, 23, 10, 0, 0, DateTimeKind.Utc);

        var oldestId = await AddNotification(app, readerId, date);
        var newerId = await AddNotification(app, readerId, date.AddHours(1));
        var newestId = await AddNotification(app, readerId, date.AddHours(1));
        await AddNotification(app, otherUserId, date.AddHours(2));

        var firstPage = await reader.GetFromJsonAsync<NotificationPageResponse>(
            "/api/notifications?page=1&pageSize=2");
        var secondPage = await reader.GetFromJsonAsync<NotificationPageResponse>(
            "/api/notifications?page=2&pageSize=2");

        Assert.NotNull(firstPage);
        Assert.NotNull(secondPage);
        Assert.Equal(1, firstPage.Page);
        Assert.Equal(2, firstPage.PageSize);
        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(new[] { newestId, newerId }, firstPage.Items.Select(item => item.Id));
        Assert.Equal("OfferReceived", firstPage.Items[0].Type);
        Assert.Equal("/offers", firstPage.Items[0].Link);

        Assert.Equal(2, secondPage.Page);
        Assert.Equal(3, secondPage.TotalCount);
        Assert.Equal(oldestId, Assert.Single(secondPage.Items).Id);
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?pageSize=0")]
    [InlineData("?pageSize=101")]
    [InlineData("?page=2147483647&pageSize=100")]
    public async Task List_rejects_invalid_pagination(string query)
    {
        using var app = new SocialApp();
        await app.Seed();
        using var reader = app.Client();
        await app.Register(reader);

        using var response = await reader.GetAsync($"/api/notifications{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Marking_one_notification_as_read_reduces_count_and_keeps_first_timestamp()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var reader = app.Client();

        var readerId = await app.Register(reader);
        var notificationId = await AddNotification(app, readerId, DateTime.UtcNow);
        await AddNotification(app, readerId, DateTime.UtcNow);

        Assert.Equal(2, await GetUnreadCount(reader));

        using var first = await Patch(reader, $"/api/notifications/{notificationId}/read");
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(1, await GetUnreadCount(reader));

        var firstReadAt = await GetReadAt(app, notificationId);
        Assert.NotNull(firstReadAt);

        using var second = await Patch(reader, $"/api/notifications/{notificationId}/read");
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
        Assert.Equal(1, await GetUnreadCount(reader));
        Assert.Equal(firstReadAt, await GetReadAt(app, notificationId));
    }

    [Fact]
    public async Task Marking_another_users_notification_as_read_returns_not_found()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var reader = app.Client();
        using var owner = app.Client();

        await app.Register(reader);
        var ownerId = await app.Register(owner);
        var notificationId = await AddNotification(app, ownerId, DateTime.UtcNow);

        using var foreign = await Patch(reader, $"/api/notifications/{notificationId}/read");
        using var missing = await Patch(reader, "/api/notifications/-1/read");

        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(0, await GetUnreadCount(reader));
        Assert.Equal(1, await GetUnreadCount(owner));
        Assert.Null(await GetReadAt(app, notificationId));
    }

    [Fact]
    public async Task Marking_all_as_read_changes_only_the_current_users_unread_notifications()
    {
        using var app = new SocialApp();
        await app.Seed();
        using var reader = app.Client();
        using var otherUser = app.Client();

        var readerId = await app.Register(reader);
        var otherUserId = await app.Register(otherUser);
        var previousReadAt = new DateTime(
            2026, 11, 23, 11, 0, 0, DateTimeKind.Utc);

        var firstId = await AddNotification(app, readerId, previousReadAt.AddHours(-2));
        var secondId = await AddNotification(app, readerId, previousReadAt.AddHours(-1));
        var alreadyReadId = await AddNotification(
            app, readerId, previousReadAt.AddHours(-3), previousReadAt);
        var foreignId = await AddNotification(app, otherUserId, previousReadAt);

        Assert.Equal(2, await GetUnreadCount(reader));

        using var response = await Patch(reader, "/api/notifications/read-all");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, await GetUnreadCount(reader));
        Assert.Equal(1, await GetUnreadCount(otherUser));
        Assert.NotNull(await GetReadAt(app, firstId));
        Assert.NotNull(await GetReadAt(app, secondId));
        Assert.Equal(previousReadAt, await GetReadAt(app, alreadyReadId));
        Assert.Null(await GetReadAt(app, foreignId));
    }

    private static async Task<int> GetUnreadCount(HttpClient client)
    {
        var response = await client.GetFromJsonAsync<NotificationUnreadCountResponse>(
            "/api/notifications/unread-count");

        Assert.NotNull(response);
        return response.UnreadCount;
    }

    private static async Task<HttpResponseMessage> Patch(HttpClient client, string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, path);
        return await client.SendAsync(request);
    }

    private static async Task<int> AddNotification(
        SocialApp app,
        int userId,
        DateTime createdAtUtc,
        DateTime? readAtUtc = null)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ArtistoDbContext>();

        var notification = new Notification
        {
            UserId = userId,
            Type = NotificationType.OfferReceived,
            Message = "You received an offer.",
            Link = "/offers",
            CreatedAtUtc = createdAtUtc,
            ReadAtUtc = readAtUtc
        };

        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        return notification.Id;
    }

    private static async Task<DateTime?> GetReadAt(SocialApp app, int notificationId)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ArtistoDbContext>();

        return await db.Notifications
            .Where(notification => notification.Id == notificationId)
            .Select(notification => notification.ReadAtUtc)
            .SingleAsync();
    }
}