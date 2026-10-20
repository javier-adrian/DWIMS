using DWIMS.Data;

namespace DWIMS.Service.Department.Dtos;

public sealed record RoleDto(
    Guid id,
    string Title,
    GeneralRole GeneralRole,
    string? Department);