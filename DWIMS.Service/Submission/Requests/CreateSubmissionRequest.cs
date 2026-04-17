using DWIMS.Service.Submission.Dtos;

namespace DWIMS.Service.Submission.Requests;

public sealed class CreateSubmissionRequest
{
    public required Guid ProcessId { get; init; }
    public required IReadOnlyList<FieldValueInputDto> Fields { get; init; }
}