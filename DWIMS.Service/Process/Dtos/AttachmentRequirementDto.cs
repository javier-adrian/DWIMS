namespace DWIMS.Service.Process.Dtos;

public sealed record AttachmentRequirementDto(
    Guid Id,
    string Title,
    int Order);