namespace DWIMS.Service.Submission.Dtos;

public sealed record FieldValueInputDto(
    Guid FieldId,
    string? FieldValue);