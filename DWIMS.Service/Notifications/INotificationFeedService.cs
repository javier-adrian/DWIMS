using DWIMS.Service.Common.Dtos;

namespace DWIMS.Service.Common;

public interface INotificationFeedService
{
    Task<Result<NotificationListDto>> GetNotificationsAsync(
        int page = 1, 
        int pageSize = 20, 
        CancellationToken cancellationToken = default);
    
    Task<Result> MarkReadAsync(Guid notificationId, CancellationToken cancellationToken = default);
    
    Task<Result> MarkAllReadAsync(CancellationToken cancellationToken = default);
}