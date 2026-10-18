using DWIMS.Data;
using DWIMS.Service.Department.Dtos;

namespace DWIMS.Service.Admin.Dtos;

public sealed record UserDirectoryDto(
    string FirstName,
    string LastName,
    string Email,
    Guid UserId,
    string? Contact,
    bool HasPassword,
    bool HasSignature,
    bool Active,
    IEnumerable<RoleDto> Roles,
    int Assignments);