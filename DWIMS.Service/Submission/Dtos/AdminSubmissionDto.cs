using DWIMS.Data;

namespace DWIMS.Service.Submission.Dtos;

public sealed record AdminSubmissionDto(
    Guid Id,
    string VerificationCode,
    string ProcessTitle,
    Status Status,
    DateTime SubmittedOn,
    DateTime? CompletedOn,
    string SubmitterName,
    string StepTitle,
    string LatestReviewer);