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
        var processes = await context.Processes
            .Include(p => p.Department)
            .Select(p => new ProcessSummaryDto(
                p.Id,
                p.DepartmentId,
                p.Department.Title,
                p.Title,
                null,
                p.Steps.Count,
                p.DocumentId != Guid.Empty
            ))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ProcessSummaryDto>>.Success(processes);
    }

    public async Task<Result<ProcessDetailDto>> GetProcessAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var process = await context.Processes
            .Include(p => p.Department)
            .Include(p => p.Steps.OrderBy(s => s.Order))
            .ThenInclude(s => s.Department)
            .Include(p => p.Fields)
            .Where(p => p.Id == id)
            .Select(p => new ProcessDetailDto(
                p.Id,
                p.Title,
                null,
                p.DepartmentId,
                p.Department.Title,
                p.Steps.Select(s => new StepDto(
                    s.Id,
                    s.Order,
                    s.Title,
                    s.Role,
                    s.DepartmentId,
                    s.Department.Title)).ToList(),
                p.Fields.Select(f => new FieldDto(
                    f.Id,
                    f.Title,
                    f.Type,
                    f.Required)).ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (process is null)
            return Result<ProcessDetailDto>.Failure("PROCESS_NOT_FOUND", "Process not found.");

        return Result<ProcessDetailDto>.Success(process);
    }

    public async Task<Result<Guid>> CreateProcessAsync(CreateProcessRequest request, CancellationToken cancellationToken = default)
    {
        if (!currentUserService.HasRoleInDepartment(request.DepartmentId, GeneralRole.Administrator) &&
            !currentUserService.isSuperAdministrator)
            return Result<Guid>.Failure("FORBIDDEN", "You do not have administrator access to this department.");
        
        var department = await context.Departments
            .FirstOrDefaultAsync(x => x.Id == request.DepartmentId, cancellationToken);

        if (department is null)
            return Result<Guid>.Failure("DEPARTMENT_NOT_FOUND", "Department not found.");

        var document = new Document
        {
            Id = Guid.NewGuid(),
            Link = ""
        };

        context.Documents.Add(document);

        var process = new Data.Process
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            DepartmentId = request.DepartmentId,
            DocumentId = document.Id
        };
        
        context.Processes.Add(process);
        
        await context.SaveChangesAsync(cancellationToken);
        
        return Result<Guid>.Success(process.Id);
    }

    public async Task<Result> UpdateProcessAsync(Guid id, UpdateProcessRequest request, CancellationToken cancellationToken = default)
    {
        var department = await context.Processes
            .Where(x => x.Id == id)
            .Select(x => x.DepartmentId)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (!currentUserService.HasRoleInDepartment(department, GeneralRole.Administrator) &&
            !currentUserService.isSuperAdministrator)
            return Result.Failure("FORBIDDEN", "You do not have administrator access to this department.");
        
        throw new NotImplementedException();
    }

    public async Task<Result> DeleteProcessAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var department = await context.Processes
            .Where(x => x.Id == id)
            .Select(x => x.DepartmentId)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (!currentUserService.HasRoleInDepartment(department, GeneralRole.Administrator) &&
            !currentUserService.isSuperAdministrator)
            return Result.Failure("FORBIDDEN", "You do not have administrator access to this department.");
        
        throw new NotImplementedException();
    }

    public async Task<Result<Guid>> AddStepAsync(Guid processId, AddStepRequest request, CancellationToken cancellationToken = default)
    {
        // var department = await context.Processes
        //     .Where(x => x.Id == processId)
        //     .Select(x => x.DepartmentId)
        //     .FirstOrDefaultAsync(cancellationToken);
        //
        // if (!currentUserService.HasRoleInDepartment(department, GeneralRole.Administrator))
        //     return Result<Guid>.Failure("FORBIDDEN", "You do not have administrator access to this department.");

        var step = new Step
        {
            Id = Guid.NewGuid(),
            Order = context.Steps.Count(x => x.Id == processId) + 1,
            Title = request.Title,
            DepartmentId = request.DepartmentId,
            Role = request.Role
        };
        
        context.Steps.Add(step);
        
        await context.SaveChangesAsync(cancellationToken);
        
        return Result<Guid>.Success(step.Id);
    }

    public async Task<Result> UpdateStepAsync(Guid processId, Guid stepId, UpdateStepRequest request,
        CancellationToken cancellationToken = default)
    {
        var department = await context.Processes
            .Where(x => x.Id == processId)
            .Select(x => x.DepartmentId)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (!currentUserService.HasRoleInDepartment(department, GeneralRole.Administrator) &&
            !currentUserService.isSuperAdministrator)
            return Result.Failure("FORBIDDEN", "You do not have administrator access to this department.");
        
        throw new NotImplementedException();
    }

    public async Task<Result> DeleteStepAsync(Guid processId, Guid stepId, CancellationToken cancellationToken = default)
    {
        var department = await context.Processes
            .Where(x => x.Id == processId)
            .Select(x => x.DepartmentId)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (!currentUserService.HasRoleInDepartment(department, GeneralRole.Administrator) &&
            !currentUserService.isSuperAdministrator)
            return Result.Failure("FORBIDDEN", "You do not have administrator access to this department.");
        
        throw new NotImplementedException();
    }

    public async Task<Result<Guid>> AddFieldAsync(Guid processId, AddFieldRequest request, CancellationToken cancellationToken = default)
    {
        var department = await context.Processes
            .Where(x => x.Id == processId)
            .Select(x => x.DepartmentId)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (!currentUserService.HasRoleInDepartment(department, GeneralRole.Administrator) &&
            !currentUserService.isSuperAdministrator)
            return Result<Guid>.Failure("FORBIDDEN", "You do not have administrator access to this department.");

        var process = await context.Processes
            .FirstOrDefaultAsync(x => x.Id == processId, cancellationToken);
        
        if (process is null)
            return Result<Guid>.Failure("PROCESS_NOT_FOUND", "Process not found.");

        var field = new Field
        {
            Id = Guid.NewGuid(),
            ProcessId = processId,
            Title = request.Title,
            Type = request.InputType,
            Required = request.Required,
        };
        
        context.Fields.Add(field);
        
        await context.SaveChangesAsync(cancellationToken);
        
        return Result<Guid>.Success(field.Id);
    }
}