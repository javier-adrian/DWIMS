using DWIMS.Data;
using DWIMS.Service.Admin;
using DWIMS.Service.Admin.Dtos;
using DWIMS.Service.Admin.Requests;
using DWIMS.Service.Common;
using DWIMS.Service.Roles.Dtos;
using DWIMS.Service.Logs;
using DWIMS.Service.User;
using Microsoft.EntityFrameworkCore;

namespace DWIMS.Service.Services;

public class UserAdministrationService(
    AppDbContext context,
    ILogService logService,
    ICurrentUserService currentUser): IUserAdministrationService
{
    public async Task<Result<PagedResult<UserDirectoryDto>>> GetUsersAsync(UserFilterRequest request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId == null) 
            return Result<PagedResult<UserDirectoryDto>>
                .Failure("UNAUTHORIZED", "User is not authenticated.");
        
        if (!currentUser.isSuperAdministrator)
            return Result<PagedResult<UserDirectoryDto>>
                .Failure("UNAUTHORIZED", "User is not authorized.");
        
        var page = Math.Max(request.Page, 1);
        
        var query = request.IsActive switch
        {
            true => context.Users.AsQueryable(),
            false => context.Users.AsQueryable().IgnoreQueryFilters().Where(x => x.IsDeleted),
            _ => context.Users.IgnoreQueryFilters()
        };
        
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(x => 
                x.FirstName.Contains(request.Search) || 
                x.LastName.Contains(request.Search) ||
                x.Email.Contains(request.Search));
        
        var count = await query.CountAsync(cancellationToken);

        var items = await Project(query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ThenBy(u => u.Id)
            .Skip((page - 1) * request.PageSize)
            .Take(request.PageSize))
            .ToListAsync(cancellationToken);
            
        return Result<PagedResult<UserDirectoryDto>>
            .Success(new PagedResult<UserDirectoryDto>(items, count, page, request.PageSize));
    }

    public async Task<Result<UserDirectoryDto>> GetUserAsync(Guid id, CancellationToken cancellationToken)
    {
        if (currentUser.UserId == null) 
            return Result<UserDirectoryDto>
                .Failure("UNAUTHORIZED", "User is not authenticated.");
        
        if (!currentUser.isSuperAdministrator)
            return Result<UserDirectoryDto>
                .Failure("UNAUTHORIZED", "User is not authorized.");

        var user = await Project(context.Users
            .AsQueryable()
            .IgnoreQueryFilters()
            .Where(u => u.Id == id)).FirstOrDefaultAsync(cancellationToken);
        
        if (user == null)
            return Result<UserDirectoryDto>
                .Failure("USER_NOT_FOUND", "User not found.");

        return Result<UserDirectoryDto>
            .Success(user);
    }

    public async Task<Result<Guid>> DeleteUserAsync(Guid id, CancellationToken cancellationToken)
    {
        if (currentUser.UserId == null) 
            return Result<Guid>
                .Failure("UNAUTHORIZED", "User is not authenticated.");
        
        var user = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id, cancellationToken: cancellationToken);
        
        if (user == null)
            return Result<Guid>
                .Failure("USER_NOT_FOUND", "User not found.");
        
        if (user.IsDeleted)
            return Result<Guid>
                .Failure("USER_ALREADY_DELETED", "User already deactivated.");

        if (id == currentUser.UserId)
            return Result<Guid>
                .Failure("CANNOT_DEACTIVATE_SELF", "Cannot deactivate self.");
        
        if (!currentUser.isSuperAdministrator)
            return Result<Guid>
                .Failure("UNAUTHORIZED", "User is not authorized.");

        var isSuperAdmin = await context.Roles
            .AnyAsync(
                r => 
                    r.UserId == id && 
                    !r.IsDeleted && 
                    r.GeneralRole == GeneralRole.SuperAdministrator, 
                cancellationToken: cancellationToken);

        if (isSuperAdmin)
        {
            var otherSuperAdmins = await context.Users.AnyAsync(
                u => u.Id != id &&
                     u.Roles.Any(r => r.GeneralRole == GeneralRole.SuperAdministrator && !r.IsDeleted),
                cancellationToken);
            
            if (!otherSuperAdmins)
                return Result<Guid>
                    .Failure("SUPER_ADMIN_REQUIRED", "Cannot deactivate last super administrator.");
        }

        var hasOpenAssignments = await context.Responses
            .AnyAsync(r => r.AssigneeId == id && r.CompletedOn == null, cancellationToken: cancellationToken);

        if (hasOpenAssignments)
            return Result<Guid>
                .Failure("HAS_OPEN_ASSIGNMENTS", "Cannot deactivate user with open assignments.");
        
        var refreshTokens = await context.RefreshTokens
            .Where(t => t.UserId == id && !t.Revoked)
            .ToListAsync(cancellationToken: cancellationToken);

        foreach (var refreshToken in refreshTokens)
            refreshToken.Revoked = true;
        
        user.IsDeleted = true;
        user.DeletedOn = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        
        await logService.LogAsync("USER_DEACTIVATED", "User", user.Id, cancellationToken: cancellationToken);

        return Result<Guid>
            .Success(id);
    }

    public async Task<Result<Guid>> ReactivateUserAsync(Guid id, CancellationToken cancellationToken)
    {
        if (currentUser.UserId == null) 
            return Result<Guid>
                .Failure("UNAUTHORIZED", "User is not authenticated.");
        
        if (!currentUser.isSuperAdministrator)
            return Result<Guid>
                .Failure("UNAUTHORIZED", "User is not authorized.");
        
        var user = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id, cancellationToken: cancellationToken);
        
        if (user == null)
            return Result<Guid>
                .Failure("USER_NOT_FOUND", "User not found.");
        
        if (!user.IsDeleted)
            return Result<Guid>
                .Failure("USER_ACTIVE", "User already active.");
        
        user.IsDeleted = false;
        user.DeletedOn = null;

        await context.SaveChangesAsync(cancellationToken);
        
        await logService.LogAsync("USER_REACTIVATED", "User", user.Id, cancellationToken: cancellationToken);

        return Result<Guid>
            .Success(id);
    }

    private IQueryable<UserDirectoryDto> Project(IQueryable<Data.User> users) =>
        users.Select(u => new UserDirectoryDto(
            u.FirstName,
            u.LastName,
            u.Email,
            u.Id,
            u.ContactNumber,
            u.Password != null,
            u.Signature != null,
            !u.IsDeleted,
            u.Roles
                .Where(r => !r.IsDeleted)
                .Select(r => new RoleDto(
                    r.Id,
                    r.Title,
                    r.GeneralRole)),
            context.Responses.Count(r => r.AssigneeId == u.Id && r.CompletedOn == null)
        ));
}