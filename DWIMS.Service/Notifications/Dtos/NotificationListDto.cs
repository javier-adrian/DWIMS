namespace DWIMS.Service.Common.Dtos;

public sealed record NotificationListDto(
    IReadOnlyList<NotificationDto> Items,
    int UnreadCount,
    int Total);