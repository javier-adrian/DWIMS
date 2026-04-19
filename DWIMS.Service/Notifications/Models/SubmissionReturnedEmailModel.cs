namespace DWIMS.Service.Common.Models;

public sealed class SubmissionReturnedEmailModel : EmailTemplateModel
{
    public required string ProcessTitle { get; init; }
    public required string StepTitle { get; init; }
    public required string Remarks { get; init; }
}