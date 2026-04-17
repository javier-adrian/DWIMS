namespace DWIMS.Service.Process.Dtos;

public sealed record ProcessDetailDto(
    Guid Id,
    string Name,
    string? Description,
    Guid DepartmentId,
    string DepartmentName,
    IReadOnlyList<StepDto> Steps,
    IReadOnlyList<FieldDto> Fields);