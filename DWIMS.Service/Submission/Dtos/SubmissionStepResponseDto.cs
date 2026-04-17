namespace DWIMS.Service.Submission.Dtos;

public sealed record SubmissionStepResponseDto(
    Guid Id,
    string StepName,
    string? Outcome,
    string? Remarks,
    string? Reviewer,
    DateTime ActivatedAt,
    DateTime? CompletedAt);