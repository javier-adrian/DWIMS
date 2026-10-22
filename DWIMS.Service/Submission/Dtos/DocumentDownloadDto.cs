namespace DWIMS.Service.Submission.Dtos;

public record DocumentDownloadDto(
    Stream Content,
    string FileName,
    string ContentType);