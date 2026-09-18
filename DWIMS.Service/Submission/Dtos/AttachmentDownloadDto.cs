namespace DWIMS.Service.Submission.Dtos;

public sealed record AttachmentDownloadDto(
    Stream Content,
    string FileName,
    string ContentType);