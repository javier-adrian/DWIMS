using DWIMS.Data;

namespace DWIMS.Service.Roles.Dtos;

public sealed record RoleDto(
    Guid id,
    string Title,
    GeneralRole GeneralRole);