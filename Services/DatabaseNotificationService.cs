using ARTISTO.Data;
using ARTISTO.Models;

namespace ARTISTO.Services;

public sealed class DatabaseNotificationService : INotificationService
{
    private readonly ArtistoDbContext _db;

    public DatabaseNotificationService(ArtistoDbContext db)
    {
        _db = db;
    }

    public Task NotifyAsync(
        int userId,
        NotificationType type,
        string message,
        string? link = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Message = message,
            Link = link,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.Notifications.Add(notification);

        return Task.CompletedTask;
    }
}