using DWIMS.Data;
using DWIMS.Service.Common;
using DWIMS.Service.Roles;
using DWIMS.Service.Roles.Requests;
using DWIMS.Service.User;
using Microsoft.EntityFrameworkCore;

namespace DWIMS.Service.Services;

public class RoleService(
    AppDbContext context,
    ICurrentUserService currentUser) : IRoleService
{
    public async Task<Result> AssignRoleAsync(AssignRoleRequest request, CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);

        if (user is null)
            return Result.Failure("USER_NOT_FOUND", "User not found.");
        
        if (request.GeneralRole == GeneralRole.SuperAdministrator)
        {
            var existingRole = await context.Roles
                .FirstOrDefaultAsync(x =>
                    x.UserId == user.Id &&
                    x.GeneralRole == GeneralRole.SuperAdministrator,
                    cancellationToken);

            if (existingRole is not null)
                return Result.Success();

            context.Roles.Add(new Role
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                GeneralRole = GeneralRole.SuperAdministrator,
                Title = "Super Administrator",
                Description = "Top level administrator."
            });

            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        if (!currentUser.isSuperAdministrator)
            return Result.Failure("FORBIDDEN", "Only a super administrator can assign global roles.");
        
        if (request.GeneralRole is not (GeneralRole.Reviewer or GeneralRole.Administrator))
            return Result.Failure("INVALID_ROLE", "Only a reviewer or administrator role can be assigned.");
        
        var existingGlobal = await context.Roles
            .FirstOrDefaultAsync(x => 
                x.UserId == user.Id &&
                x.GeneralRole == request.GeneralRole, cancellationToken);

        if (existingGlobal is not null)
        {
            existingGlobal.IsDeleted = false;
            existingGlobal.DeletedOn = null;
        }
        else
        {
            context.Roles.Add(new Role
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                GeneralRole = request.GeneralRole,
                Title = request.GeneralRole.ToString(),
                Description = $"Global {request.GeneralRole.ToString()} assigned by a super administrator."
            });
        }
        
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> RemoveRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await context.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

        if (role is null)
            return Result.Failure("ROLE_NOT_FOUND", "Role not found.");

        context.Roles.Remove(role);
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}