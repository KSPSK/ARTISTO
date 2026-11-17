using System.Collections.Concurrent;
using ARTISTO.Services;

namespace ARTISTO.Tests;

public sealed record SentNotification(int UserId, NotificationType Type, string Message, string? Link);

// Replaces the real notification service in tests so we can check which events were sent.
public sealed class RecordingNotificationService : INotificationService
{
    private readonly ConcurrentQueue<SentNotification> _sent = new();

    public IReadOnlyList<SentNotification> Sent => _sent.ToList();

    public Task NotifyAsync(int userId, NotificationType type, string message, string? link = null)
    {
        _sent.Enqueue(new SentNotification(userId, type, message, link));
        return Task.CompletedTask;
    }
}