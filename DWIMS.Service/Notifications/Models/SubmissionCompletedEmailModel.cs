using DWIMS.Data;

namespace DWIMS.Service.Common.Models;

public class SubmissionCompletedEmailModel : EmailTemplateModel
{
    public required string ProcessTitle { get; init; }
    public required Status Status { get; init; }
    public required DateTime Timestamp { get; init; }
}