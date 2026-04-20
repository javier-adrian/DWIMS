namespace DWIMS.Service.Logs.Dtos;

public sealed record LogDto( 
    Guid Id,
    Guid UserId,
    string? UserName,
    string Action, 
    string? EntityType,
    Guid? EntityId,
    DateTime Timestamp,
    string? Metadata,
    string? IpAddress);