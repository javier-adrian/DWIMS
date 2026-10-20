using DWIMS.Data;

namespace DWIMS.Service.Submission.Requests;

public sealed class RespondToStepRequest
{
    public required Status Outcome { get; init; }
    public string? Remarks { get; init; }
}