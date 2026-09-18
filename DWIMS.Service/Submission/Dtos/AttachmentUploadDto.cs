namespace DWIMS.Service.Submission.Dtos;

public sealed record AttachmentUploadDto(
    Stream Content,
    string FileName,
    string ContentType);