namespace ARTISTO.Services;

public enum NotificationType
{
    OfferReceived,
    OfferAccepted,
    OfferDeclined,
    OfferCountered,
    OrderPlaced,
    OrderHandedOver,
    OrderCompleted,
    OrderCancelled
}

public interface INotificationService
{
    Task NotifyAsync(int userId, NotificationType type, string message, string? link = null);
}