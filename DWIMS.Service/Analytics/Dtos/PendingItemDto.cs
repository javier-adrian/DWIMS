namespace DWIMS.Service.Analytics.Dtos;

public sealed record PendingItemDto(
    string ProcessName,
    string StepName, 
    string AssigneeName,
    double WaitingHours,
    bool OnHold,
    Guid SubmissionId);