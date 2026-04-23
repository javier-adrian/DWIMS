using DWIMS.Data;

namespace DWIMS.Service.Process.Dtos;

public sealed record StepDto(
    Guid Id,
    int Order,
    string Name,
    GeneralRole Role,
    Guid? DepartmentId,
    string? DepartmentName);