namespace DWIMS.Service.Process.Requests;

public class UploadDocumentRequest
{
    public required Stream File { get; init; }
    public required string FileName { get; init; }
}