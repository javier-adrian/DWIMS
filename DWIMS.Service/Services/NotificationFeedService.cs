using DWIMS.Data;
using DWIMS.Service.Common;
using DWIMS.Service.Common.Dtos;
using DWIMS.Service.User;
using Microsoft.EntityFrameworkCore;

namespace DWIMS.Service.Services;

public class NotificationFeedService(
    AppDbContext context, 
    ICurrentUserService currentUser) : INotificationFeedService
{
    public async Task<Result<NotificationListDto>> GetNotificationsAsync(
        int page = 1, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Result<NotificationListDto>.Failure("UNAUTHORIZED", "User is not authenticated.");
        
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = context.Notifications
            .Where(n => n.UserId == currentUser.UserId);
        
        var unreadCount = await query.CountAsync(n => n.ReadOn == null, cancellationToken);
        var total = await query.CountAsync(cancellationToken);
        
        var notifications = await query
                .OrderByDescending(n => n.CreatedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto(
                n.Id,
                (int)n.Kind,
                n.Title,
                n.Message,
                n.Link,
                n.CreatedOn,
                n.ReadOn))
            .ToListAsync(cancellationToken);
        
        var notificationListDto = new NotificationListDto(
            notifications,
            unreadCount, 
            total);

        return Result<NotificationListDto>.Success(notificationListDto);
    }

    public async Task<Result> MarkReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Result.Failure("UNAUTHORIZED", "User is not authenticated.");
        
        var notification = await context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == currentUser.UserId, cancellationToken);
        
        if (notification is null)
            return Result.Failure("NOT_FOUND", "Notification not found.");
        
        if (notification.ReadOn is not null)
            return Result.Success();

        notification.ReadOn = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> MarkAllReadAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Result.Failure("UNAUTHORIZED", "User is not authenticated.");

        var unread = await context.Notifications
            .Where(n => n.UserId == currentUser.UserId && n.ReadOn == null)
            .ToListAsync(cancellationToken);
        
        foreach (var notification in unread)
            notification.ReadOn = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}