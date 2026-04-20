using DWIMS.Data;

namespace DWIMS.Service.Department.Requests;

public sealed class AssignRoleRequest
{
    public required string Email { get; set; }
    public required GeneralRole GeneralRole { get; set; }
    public required Guid DepartmentId { get; set; }
}