namespace DWIMS.Service.Process.Requests;

public sealed class UpdateAttachmentRequirementRequest
{
    public required string Title { get; set; }
    public required int Order { get; set; }
}