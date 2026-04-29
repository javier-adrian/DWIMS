using DWIMS.Data;
using DWIMS.Service.Common;
using DWIMS.Service.Process;
using DWIMS.Service.Process.Dtos;
using DWIMS.Service.Process.Requests;
using DWIMS.Service.Storage;
using DWIMS.Service.User;
using Microsoft.EntityFrameworkCore;

namespace DWIMS.Service.Services;

public class ProcessService(AppDbContext context, ICurrentUserService currentUserService, IStorageService storageService, IAcroFormService acroFormService) : IProcessService
{
    public async Task<Result<IReadOnlyList<ProcessSummaryDto>>> GetProcessesAsync(CancellationToken cancellationToken = default)
    {
        var departmentIds = currentUserService.isSuperAdministrator
            ? null
            : currentUserService.Roles
                .Where(r => r.Key != Guid.Empty && r.Value >= GeneralRole.Administrator)
                .Select(r => r.Key)
                .ToList();

        if (departmentIds is not null && departmentIds.Count == 0)
            return Result<IReadOnlyList<ProcessSummaryDto>>.Success([]);

        var processes = await context.Processes
            .Include(p => p.Department)
            .Where(p => !p.IsDeleted)
            .Where(p => departmentIds == null || departmentIds.Contains(p.DepartmentId))
            .Select(p => new ProcessSummaryDto(
                p.Id,
                p.DepartmentId,
                p.Department.Title,
                p.Title,
                null,
                p.Steps.Count,
                context.Documents.Any(d => d.ProcessId == p.Id && !d.IsDeleted)
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
                p.Steps.Where(s => !s.IsDeleted).Select(s => new StepDto(
                    s.Id,
                    s.Order,
                    s.Title,
                    s.Role,
                    s.DepartmentId,
                    s.Department.Title)).ToList(),
                p.Fields.Where(f => !f.IsDeleted).Select(f => new FieldDto(
                    f.Id,
                    f.Title,
                    f.AcroFormKey,
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

        var process = new Data.Process
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            DepartmentId = request.DepartmentId,
        };

        var document = new Document
        {
            Id = Guid.NewGuid(),
            Link = "",
            ProcessId = process.Id
        };

        context.Documents.Add(document);
        
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

        if (department == Guid.Empty)
            return Result.Failure("PROCESS_NOT_FOUND", "Process not found.");

        if (!currentUserService.HasRoleInDepartment(department, GeneralRole.Administrator) &&
            !currentUserService.isSuperAdministrator)
            return Result.Failure("FORBIDDEN", "You do not have administrator access to this department.");

        var submissions = await context.Submissions
            .Include(s => s.Inputs)
            .Include(s => s.Responses)
            .Where(s => s.ProcessId == id && !s.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var submission in submissions)
        {
            context.Inputs.RemoveRange(submission.Inputs.Where(i => !i.IsDeleted));
            context.Responses.RemoveRange(submission.Responses.Where(r => !r.IsDeleted));
            context.Submissions.Remove(submission);
        }

        var documents = await context.Documents
            .Where(d => d.ProcessId == id && !d.IsDeleted)
            .ToListAsync(cancellationToken);

        context.Documents.RemoveRange(documents);

        var fields = await context.Fields
            .Where(f => f.ProcessId == id && !f.IsDeleted)
            .ToListAsync(cancellationToken);

        context.Fields.RemoveRange(fields);

        var steps = await context.Steps
            .Where(s => s.ProcessId == id && !s.IsDeleted)
            .ToListAsync(cancellationToken);

        context.Steps.RemoveRange(steps);

        var process = await context.Processes
            .FirstAsync(p => p.Id == id, cancellationToken);

        context.Processes.Remove(process);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
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
            Order = context.Steps.Count(x => x.ProcessId == processId) + 1,
            Title = request.Title,
            DepartmentId = request.DepartmentId,
            Role = request.Role,
            ProcessId = processId
        };
        
        context.Steps.Add(step);
        
        await context.SaveChangesAsync(cancellationToken);
        
        return Result<Guid>.Success(step.Id);
    }

    public async Task<Result> UpdateStepAsync(Guid processId, Guid stepId, UpdateStepRequest request,
        CancellationToken cancellationToken = default)
    {
        var step = await context.Steps
            .FirstOrDefaultAsync(s => s.Id == stepId && s.ProcessId == processId, cancellationToken);

        if (step is null)
            return Result.Failure("STEP_NOT_FOUND", "Step not found.");

        step.DepartmentId = request.DepartmentId;

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
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

    public async Task<Result<IReadOnlyList<StepDto>>> GetStepsAsync(Guid processId, CancellationToken cancellationToken = default)
    {
        var steps = await context.Steps
            .Include(s => s.Department)
            .Where(s => s.ProcessId == processId && !s.IsDeleted)
            .OrderBy(s => s.Order)
            .Select(s => new StepDto(
                s.Id,
                s.Order,
                s.Title,
                s.Role,
                s.DepartmentId,
                s.Department.Title))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<StepDto>>.Success(steps);
    }

    public async Task<Result<Guid>> UploadDocumentAsync(Guid processId, UploadDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var process = await context.Processes
            .Include(p => p.Documents)
            .Include(p => p.Steps)
            .Include(p => p.Fields)
            .FirstOrDefaultAsync(p => p.Id == processId, cancellationToken);

        if (process is null)
            return Result<Guid>.Failure("PROCESS_NOT_FOUND", "Process not found.");

        context.Steps.RemoveRange(process.Steps);
        process.Fields.ToList().ForEach(f => context.Fields.Remove(f));

        using var ms = new MemoryStream();
        await request.File.CopyToAsync(ms, cancellationToken);
        ms.Position = 0;

        var extractionResult = await acroFormService.ExtractFieldsAsync(ms, cancellationToken);

        if (!extractionResult.IsSuccess)
            return Result<Guid>.Failure(extractionResult.Error!, extractionResult.ErrorDescription!);

        ms.Position = 0;

        var document = process.Documents.FirstOrDefault();

        if (document is null)
        {
            document = new Document
            {
                Id = Guid.NewGuid(),
                Link = "",
                ProcessId = processId
            };
            context.Documents.Add(document);
        }

        var storageKey = await storageService.UploadAsync(
            ms,
            request.FileName,
            "application/octet-stream",
            cancellationToken);

        document.Link = storageKey;

        foreach (var acroField in extractionResult.Data!)
        {
            if (acroField.IsSignature)
            {
                if (int.TryParse(acroField.Name.Split(':')[0], out var stepNum) && stepNum > 0)
                {
                    var stepName = acroField.Name.Contains(':')
                        ? acroField.Name[(acroField.Name.IndexOf(':') + 1)..].Trim()
                        : $"Step {stepNum}";

                    var step = new Step
                    {
                        Id = Guid.NewGuid(),
                        ProcessId = processId,
                        Order = stepNum,
                        Title = stepName,
                        Role = GeneralRole.Reviewer,
                        DepartmentId = process.DepartmentId,
                    };

                    context.Steps.Add(step);
                }
                continue;
            }

            var field = new Field
            {
                Id = Guid.NewGuid(),
                ProcessId = processId,
                Title = acroField.Title,
                AcroFormKey = acroField.Name,
                Type = InputTypes.Text,
                Required = true,
                DocumentId = document.Id,
            };

            context.Fields.Add(field);
        }

        await context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(document.Id);
    }
}