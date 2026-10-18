using DWIMS.Data;
using DWIMS.Service.Admin;
using DWIMS.Service.Admin.Dtos;
using DWIMS.Service.Admin.Requests;
using DWIMS.Service.Common;
using DWIMS.Service.Department.Dtos;
using DWIMS.Service.User;
using Microsoft.EntityFrameworkCore;

namespace DWIMS.Service.Services;

public class UserAdministrationService(
    AppDbContext context,
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
                    r.Title,
                    r.GeneralRole,
                    r.Department != null ? r.Department.Title : null)),
            context.Responses.Count(r => r.AssigneeId == u.Id && r.CompletedOn == null)
        ));
}