using DWIMS.Data;

namespace DWIMS.Service.Department.Dtos;

public sealed record OrganizationMemberDto(
    Guid Id,
    string FirstName,
    string? MiddleName,
    string LastName,
    string Email,
    Role Role
    );