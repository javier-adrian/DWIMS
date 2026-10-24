using DWIMS.Service.Common;
using DWIMS.Service.Services;

namespace DWIMS.Controllers;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        group.MapGet("/", GetNotifications)
            .WithDisplayName("Get Notifications")
            .WithSummary("Get all notifications of a user");

        group.MapPost("/{notificationId:guid}/read", ReadNotification)
            .WithDisplayName("Read Notification")
            .WithSummary("Mark a notification of a user as read");

        group.MapPost("/read-all", ReadAllNotifications)
            .WithDisplayName("Read All Notifications")
            .WithSummary("Mark all notifications of a user as read");
        
        return app;
    }

    private static async Task<IResult> ReadAllNotifications(
        INotificationFeedService notificationFeedService,
        CancellationToken cancellationToken)
    {
        var result = await notificationFeedService.MarkAllReadAsync(cancellationToken);
        
        return result.IsSuccess
            ? Results.NoContent()
            : Results.UnprocessableEntity(new { result.Error, result.ErrorDescription });
    }

    private static async Task<IResult> ReadNotification(
        Guid notificationId,
        INotificationFeedService notificationFeedService,
        CancellationToken cancellationToken)
    {
        var result = await notificationFeedService.MarkReadAsync(notificationId, cancellationToken);
        
        return result.IsSuccess
            ? Results.NoContent()
            : Results.UnprocessableEntity(new { result.Error, result.ErrorDescription });
    }

    private static async Task<IResult> GetNotifications(
        INotificationFeedService notificationFeedService,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await notificationFeedService.GetNotificationsAsync(page, pageSize, cancellationToken);

        return result.ToOkResult();
    }
}