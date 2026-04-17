using DWIMS.Data;

namespace DWIMS.Service.Submission.Dtos;

public record SubmissionDetailDto(
    Guid Id,
    Guid ProcessId,
    string ProcessName,
    Status Status,
    DateTime SubmittedAt,
    DateTime? CompletedAt,
    string? CurrentStep,
    IReadOnlyList<SubmissionStepResponseDto> StepResponses,
    IReadOnlyList<SubmissionFieldValueDto> FieldValues
    );