using DWIMS.Service.Common;
using DWIMS.Service.Roles.Requests;

namespace DWIMS.Service.Roles;

public interface IRoleService
{
    Task<Result> AssignRoleAsync(
        AssignRoleRequest request,
        CancellationToken cancellationToken = default);
    
    Task<Result> RemoveRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default);
}