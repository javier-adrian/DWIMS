namespace DWIMS.Service.Submission.Dtos;

public sealed record SubmissionFieldValueDto(
    string FieldName,
    string FieldType,
    string? FieldValue
    );