using DWIMS.Data;

namespace DWIMS.Service.Submission.Dtos;

public sealed record PublicSubmissionStatusDto(
    Guid Id,
    string ProcessName,
    Status Status,
    DateTime SubmittedAt,
    DateTime? CompletedAt,
    string? CurrentStep,
    IReadOnlyList<PublicStepStatusDto> Steps);