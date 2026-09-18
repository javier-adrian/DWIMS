namespace DWIMS.Service.Submission.Dtos;

public sealed record AttachmentDto(
    Guid Id,
    string FileName,
    string ContentType);