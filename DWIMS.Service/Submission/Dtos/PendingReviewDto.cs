using DWIMS.Data;

namespace DWIMS.Service.Submission.Dtos;

public sealed record PendingReviewDto(
    Guid Id,
    Guid ResponseId,
    string ResponseName,
    string StepName,
    string SubmitterName,
    Status Status,
    DateTime SubmittedAt,
    DateTime? StepActivatedAt
    );