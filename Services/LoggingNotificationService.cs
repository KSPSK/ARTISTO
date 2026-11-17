namespace ARTISTO.Services;

// Temporary implementation until the notifications module exists.
// It only writes the event to the application log, so callers already send every event in the right place.
public class LoggingNotificationService : INotificationService
{
    private readonly ILogger<LoggingNotificationService> _logger;

    public LoggingNotificationService(ILogger<LoggingNotificationService> logger)
    {
        _logger = logger;
    }

    public Task NotifyAsync(int userId, NotificationType type, string message, string? link = null)
    {
        _logger.LogInformation(
            "Notification {Type} for user {UserId}: {Message} ({Link})",
            type,
            userId,
            message,
            link);

        return Task.CompletedTask;
    }
}