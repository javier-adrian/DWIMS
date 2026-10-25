using DWIMS.Data;

namespace DWIMS.Service.Process.Dtos;

public sealed record ProcessDetailDto(
    Guid Id,
    string Name,
    string? Description,
    ProcessKind Kind,
    IReadOnlyList<StepDto> Steps,
    IReadOnlyList<FieldDto> Fields);