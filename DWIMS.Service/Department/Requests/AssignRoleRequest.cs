using DWIMS.Data;

namespace DWIMS.Service.Department.Requests;

public sealed class AssignRoleRequest
{
    public required Guid UserId { get; set; }
    public required GeneralRole Role { get; set; }
}