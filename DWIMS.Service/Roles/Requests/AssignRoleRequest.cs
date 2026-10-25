using DWIMS.Data;

namespace DWIMS.Service.Roles.Requests;

public sealed class AssignRoleRequest
{
    public required string Email { get; set; }
    public required GeneralRole GeneralRole { get; set; }
}