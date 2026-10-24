namespace DWIMS.Service.Common.Dtos;

public sealed record NotificationDto(
    Guid Id,
    int Kind,
    string Title,
    string Message,
    string Link,
    DateTime CreatedOn,
    DateTime? ReadOn);