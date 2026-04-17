using DWIMS.Data;
using DWIMS.Service.Common;
using DWIMS.Service.Process;
using DWIMS.Service.Process.Dtos;
using DWIMS.Service.Process.Requests;
using DWIMS.Service.User;
using Microsoft.EntityFrameworkCore;

namespace DWIMS.Service.Services;

public class ProcessService(AppDbContext context, ICurrentUserService currentUserService) : IProcessService
{
    public async Task<Result<IReadOnlyList<ProcessSummaryDto>>> GetProcessesAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<ProcessDetailDto>> GetProcessAsync(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<Guid>> CreateProcessAsync(CreateProcessRequest request, CancellationToken cancellationToken = default)
    {
        if (!currentUserService.HasRoleInDepartment(request.DepartmentId, GeneralRole.Administrator))
            return Result<Guid>.Failure("FORBIDDEN", "You do not have administrator access to this department.");
        
        var department = await context.Departments
            .FirstOrDefaultAsync(x => x.Id == request.DepartmentId, cancellationToken);

        if (department is null)
            return Result<Guid>.Failure("DEPARTMENT_NOT_FOUND", "Department not found.");

        var process = new Data.Process
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            DepartmentId = request.DepartmentId
        };
        
        context.Processes.Add(process);
        
        await context.SaveChangesAsync(cancellationToken);
        
        return Result<Guid>.Success(process.Id);
    }

    public async Task<Result> UpdateProcessAsync(Guid id, UpdateProcessRequest request, CancellationToken cancellationToken = default)
    {
        if (!currentUserService.HasRoleInDepartment(id, GeneralRole.Administrator))
            return Result.Failure("FORBIDDEN", "You do not have administrator access to this department.");
        
        throw new NotImplementedException();
    }

    public async Task<Result> DeleteProcessAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!currentUserService.HasRoleInDepartment(id, GeneralRole.Administrator))
            return Result.Failure("FORBIDDEN", "You do not have administrator access to this department.");
        
        throw new NotImplementedException();
    }

    public async Task<Result<Guid>> AddStepAsync(Guid processId, AddStepRequest request, CancellationToken cancellationToken = default)
    {
        if (!currentUserService.HasRoleInDepartment(processId, GeneralRole.Administrator))
            return Result<Guid>.Failure("FORBIDDEN", "You do not have administrator access to this department.");
        
        throw new NotImplementedException();
    }

    public async Task<Result> UpdateStepAsync(Guid processId, Guid stepId, UpdateStepRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!currentUserService.HasRoleInDepartment(processId, GeneralRole.Administrator))
            return Result.Failure("FORBIDDEN", "You do not have administrator access to this department.");
        
        throw new NotImplementedException();
    }

    public async Task<Result> DeleteStepAsync(Guid processId, Guid stepId, CancellationToken cancellationToken = default)
    {
        if (!currentUserService.HasRoleInDepartment(processId, GeneralRole.Administrator))
            return Result.Failure("FORBIDDEN", "You do not have administrator access to this department.");
        
        throw new NotImplementedException();
    }
}