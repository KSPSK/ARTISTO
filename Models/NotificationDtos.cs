namespace ARTISTO.Models;

public sealed record NotificationResponse(
    int Id,
    string Type,
    string Message,
    string? Link,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc);

public sealed record NotificationPageResponse(
    IReadOnlyList<NotificationResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record NotificationUnreadCountResponse(int UnreadCount);