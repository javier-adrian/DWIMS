namespace DWIMS.Service.Submission.Dtos;

public sealed record PendingReviewDto(
    Guid Id,
    Guid ResponseId,
    string ResponseName,
    string StepName,
    string SubmitterName,
    DateTime SubmittedAt,
    DateTime? StepActivatedAt
    );