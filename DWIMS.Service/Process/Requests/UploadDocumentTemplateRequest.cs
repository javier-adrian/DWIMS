namespace DWIMS.Service.Process.Requests;

public sealed class UploadDocumentTemplateRequest
{
    public required Stream File { get; init; }
    public required string FileName { get; init; }
    public required IReadOnlyList<Guid> Fields { get; init; }
}