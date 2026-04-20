using DWIMS.Data;

namespace DWIMS.Service.Department.Dtos;

public sealed record DepartmentMemberDto(
    Guid Id,
    Guid RoleId,
    string FirstName,
    string? MiddleName,
    string LastName,
    string Email,
    GeneralRole Role
    );