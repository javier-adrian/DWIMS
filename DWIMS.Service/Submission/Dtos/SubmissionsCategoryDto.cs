namespace DWIMS.Service.Submission.Dtos;

public sealed record SubmissionsCategoryDto(
    Guid ProcessId,
    string ProcessName,
    int Count);