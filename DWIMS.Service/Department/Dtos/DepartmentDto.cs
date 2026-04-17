namespace DWIMS.Service.Department.Dtos;

public sealed record DepartmentDto(
    Guid Id,
    string Name,
    string? Description);