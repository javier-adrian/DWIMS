using DWIMS.Data;
using DWIMS.Service.Common;
using DWIMS.Service.Department;
using DWIMS.Service.Department.Dtos;
using DWIMS.Service.Department.Requests;
using DWIMS.Service.User;
using Microsoft.EntityFrameworkCore;

namespace DWIMS.Service.Services;

public class DepartmentService(AppDbContext context, ICurrentUserService currentUserService) : IDepartmentService
{
    public async Task<Result<IReadOnlyList<DepartmentDto>>> GetDepartmentAsync(CancellationToken cancellationToken = default)
    {
        var departmentIds = currentUserService.Roles.Keys
            .Where(id => id != Guid.Empty)
            .ToList();

        if (departmentIds.Count == 0 && !currentUserService.isSuperAdministrator)
            return Result<IReadOnlyList<DepartmentDto>>.Success([]);

        var query = currentUserService.isSuperAdministrator
            ? context.Departments
            : context.Departments.Where(d => departmentIds.Contains(d.Id));

        var departments = await query
            .Select(d => new DepartmentDto(d.Id, d.Title, null))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<DepartmentDto>>.Success(departments);
    }

    public async Task<Result<Guid>> CreateDepartmentAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        var department = new Data.Department
        {
            Id = Guid.NewGuid(),
            Title = request.Name
        };
        
        context.Departments.Add(department);

        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(department.Id);
    }

    public Task<Result> UpdateDepartmentAsync(Guid id, UpdateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result> DeleteDepartmentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> AssignRoleAsync(
        Guid departmentId,
        AssignRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);

        if (user is null)
            return Result.Failure("USER_NOT_FOUND", "User not found.");
        
        if (request.GeneralRole == GeneralRole.SuperAdministrator)
        {
            var existingRole = await context.Roles
                .FirstOrDefaultAsync(x =>
                    x.UserId == request.UserId &&
                    x.GeneralRole == GeneralRole.SuperAdministrator,
                    cancellationToken);

            if (existingRole is not null)
                return Result.Success();

            context.Roles.Add(new Role
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                GeneralRole = GeneralRole.SuperAdministrator,
                Title = "Super Administrator",
                Description = "Top level administrator."
            });

            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        
        if (departmentId == Guid.Empty)
            return Result.Failure("DEPARTMENT_NOT_FOUND", "Department not found.");

        if (!currentUserService.HasRoleInDepartment(departmentId, GeneralRole.Administrator))
            return Result.Failure("FORBIDDEN", "You do not have administrator access to this department.");

        var department = await context.Departments
            .FirstOrDefaultAsync(x => x.Id == departmentId, cancellationToken);

        if (department is null)
            return Result.Failure("DEPARTMENT_NOT_FOUND", "Department not found.");

        var existingDeptRole = await context.Roles
            .FirstOrDefaultAsync(x =>
                x.UserId == request.UserId &&
                x.DepartmentId == departmentId,
                cancellationToken);

        if (existingDeptRole is not null)
            existingDeptRole.GeneralRole = request.GeneralRole;
        else
            context.Roles.Add(new Role
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                DepartmentId = departmentId,
                GeneralRole = request.GeneralRole,
                Title = request.GeneralRole.ToString(),
                Description = $"{request.GeneralRole} in {department.Title}"
            });

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<DepartmentMemberDto>>> GetMembersAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        var departmentExists = await context.Departments
            .AnyAsync(x => x.Id == departmentId, cancellationToken);

        if (!departmentExists)
            return Result<IReadOnlyList<DepartmentMemberDto>>.Failure("DEPARTMENT_NOT_FOUND", "Department not found.");

        var members = await context.Roles
            .Where(r => r.DepartmentId == departmentId)
            .Select(r => new DepartmentMemberDto(
                r.User.Id,
                r.User.FirstName,
                r.User.MiddleName,
                r.User.LastName,
                r.User.Email,
                r.GeneralRole))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<DepartmentMemberDto>>.Success(members);
    }

    public async Task<Result> RemoveRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await context.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

        if (role is null)
            return Result.Failure("ROLE_NOT_FOUND", "Role not found.");

        if (!currentUserService.isSuperAdministrator &&
            !currentUserService.HasRoleInDepartment(role.DepartmentId ?? Guid.Empty, GeneralRole.Administrator))
            return Result.Failure("FORBIDDEN", "You do not have administrator access to this role's department.");

        context.Roles.Remove(role);
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}