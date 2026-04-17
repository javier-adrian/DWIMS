using DWIMS.Data;

namespace DWIMS.Service.Submission.Dtos;

public sealed record SubmissionSummaryDto(
    Guid Id,
    string Name,
    Status Status,
    DateTime CreatedAt,
    string? CurrentStep
    );