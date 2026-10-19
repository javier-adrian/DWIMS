namespace DWIMS.Service.Submission.Dtos;

public sealed record PublicStepStatusDto(
    int Order,
    string Title,
    string State,
    string? Reviewer,
    DateTime? CompletedAt);