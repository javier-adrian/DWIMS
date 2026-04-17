using DWIMS.Data;
using DWIMS.Service.User;
using Microsoft.AspNetCore.Authorization;

namespace DWIMS.Service.Auth;

public sealed class RoleHandler(ICurrentUserService currentUser) : AuthorizationHandler<Requirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        Requirement requirement)
    {
        var satisfied = currentUser.isSuperAdministrator ||
                        currentUser.Roles.Values.Any(x => x >= requirement.Role);
        
        if (satisfied)
            context.Succeed(requirement);
        
        return Task.CompletedTask;
    }
}