namespace DWIMS.Service.Submission.Dtos;

public sealed record SubmissionRouteDto(
    Guid StepId,
    string StepTitle,
    string? AssigneeEmail,
    string? Assignee);