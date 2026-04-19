namespace DWIMS.Service.Common.Models;

public sealed class StepActivatedModel : EmailTemplateModel
{
    public required string ProcessTitle { get; init; }
    public required string StepTitle { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string StepUrl { get; init; }
    public required string StepId { get; init; }
}