using DWIMS.Data;

namespace DWIMS.Service.Department.Dtos;

public sealed record RoleDto(
    string Title,
    GeneralRole GeneralRole,
    string? Department);