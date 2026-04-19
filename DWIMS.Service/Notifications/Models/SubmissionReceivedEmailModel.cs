namespace DWIMS.Service.Common.Models;

public sealed class SubmissionReceivedEmailModel : EmailTemplateModel
{
    public required string ProcessTitle { get; init; }
    public required Guid SubmissionId { get; init; }
    public required DateTime Timestamp { get; init; }
}