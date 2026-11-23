using ARTISTO.Data;
using ARTISTO.Extensions;
using ARTISTO.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ARTISTO.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class NotificationsController(ArtistoDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<NotificationPageResponse>> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        if (page < 1 || pageSize is < 1 or > 100)
        {
            return BadRequest(new ApiErrorResponse(
                "Page must be at least 1 and page size must be between 1 and 100."));
        }

        var offset = (page - 1L) * pageSize;

        if (offset > int.MaxValue)
        {
            return BadRequest(new ApiErrorResponse("The requested page is too large."));
        }

        var query = db.Notifications
            .AsNoTracking()
            .Where(notification => notification.UserId == userId);

        var totalCount = await query.CountAsync(cancellationToken);

        var notifications = await query
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .ThenByDescending(notification => notification.Id)
            .Skip((int)offset)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = notifications
            .Select(notification => new NotificationResponse(
                notification.Id,
                notification.Type.ToString(),
                notification.Message,
                notification.Link,
                notification.CreatedAtUtc,
                notification.ReadAtUtc))
            .ToList();

        return Ok(new NotificationPageResponse(items, page, pageSize, totalCount));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<NotificationUnreadCountResponse>> GetUnreadCount(
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var unreadCount = await db.Notifications.CountAsync(
            notification => notification.UserId == userId
                && notification.ReadAtUtc == null,
            cancellationToken);

        return Ok(new NotificationUnreadCountResponse(unreadCount));
    }

    [HttpPatch("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(
        int id,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var query = db.Notifications.Where(
            notification => notification.Id == id && notification.UserId == userId);

        var readAtUtc = DateTime.UtcNow;

        var updated = await query
            .Where(notification => notification.ReadAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    notification => notification.ReadAtUtc,
                    (DateTime?)readAtUtc),
                cancellationToken);

        if (updated > 0 || await query.AnyAsync(cancellationToken))
        {
            return NoContent();
        }

        return NotFound(new ApiErrorResponse("Notification not found."));
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead(
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var readAtUtc = DateTime.UtcNow;

        await db.Notifications
            .Where(notification => notification.UserId == userId
                && notification.ReadAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    notification => notification.ReadAtUtc,
                    (DateTime?)readAtUtc),
                cancellationToken);

        return NoContent();
    }
}