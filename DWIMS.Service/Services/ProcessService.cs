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
        var processes = await context.Processes
            .Where(p => !p.IsDeleted)
            .Select(p => new ProcessSummaryDto(
                p.Id,
                p.Title,
                null,
                p.Steps.Count,
                context.Documents.Any(d => d.ProcessId == p.Id && !d.IsDeleted),
                p.Kind
            ))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ProcessSummaryDto>>.Success(processes);
    }

    public async Task<Result<IReadOnlyList<ProcessSummaryDto>>> GetAllProcessesAsync(CancellationToken cancellationToken = default)
    {
        var processes = await context.Processes
            .Where(p => !p.IsDeleted)
            .Select(p => new ProcessSummaryDto(
                p.Id,
                p.Title,
                null,
                p.Steps.Count,
                context.Documents.Any(d => d.ProcessId == p.Id && !d.IsDeleted),
                p.Kind
            ))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ProcessSummaryDto>>.Success(processes);
    }

    public async Task<Result<ProcessDetailDto>> GetProcessAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var process = await context.Processes
            .Include(p => p.Steps.OrderBy(s => s.Order))
            .Include(p => p.Fields)
            .Where(p => p.Id == id)
            .Select(p => new ProcessDetailDto(
                p.Id,
                p.Title,
                null,
                p.Kind,
                p.Steps.Where(s => !s.IsDeleted).Select(s => new StepDto(
                    s.Id,
                    s.Order,
                    s.Title,
                    s.Role)).ToList(),
                p.Fields.Where(f => !f.IsDeleted)
                    .OrderBy(f => f.Order)
                    .ThenBy(f => f.Title)
                    .Select(f => new FieldDto(
                    f.Id,
                    f.Title,
                    f.AcroFormKey,
                    f.Type,
                    f.Required,
                    f.Options,
                    f.Order)).ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (process is null)
            return Result<ProcessDetailDto>.Failure("PROCESS_NOT_FOUND", "Process not found.");

        return Result<ProcessDetailDto>.Success(process);
    }

    public async Task<Result<Guid>> CreateProcessAsync(CreateProcessRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(request.Kind)) return Result<Guid>.Failure("INVALID_PROCESS_KIND", "Invalid process kind.");

        var process = new Data.Process
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Kind = request.Kind
        };
        
        context.Processes.Add(process);
        
        await context.SaveChangesAsync(cancellationToken);
        
        return Result<Guid>.Success(process.Id);
    }

    public async Task<Result> UpdateProcessAsync(Guid id, UpdateProcessRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Result> DeleteProcessAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!(await context.Processes
                .AnyAsync(p => 
                    p.Id == id, 
                    cancellationToken)))
            return Result.Failure("PROCESS_NOT_FOUND", "Process not found.");

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
        var step = new Step
        {
            Id = Guid.NewGuid(),
            Order = context.Steps.Count(x => x.ProcessId == processId) + 1,
            Title = request.Title,
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

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteStepAsync(Guid processId, Guid stepId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<Guid>> AddFieldAsync(Guid processId, AddFieldRequest request, CancellationToken cancellationToken = default)
    {
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
            Order = await context.Fields.Where(f => f.ProcessId == processId).MaxAsync(f => (int?)f.Order) + 1 ?? 1
        };
        
        context.Fields.Add(field);
        
        await context.SaveChangesAsync(cancellationToken);
        
        return Result<Guid>.Success(field.Id);
    }

    public async Task<Result<IReadOnlyList<StepDto>>> GetStepsAsync(Guid processId, CancellationToken cancellationToken = default)
    {
        var steps = await context.Steps
            .Where(s => s.ProcessId == processId && !s.IsDeleted)
            .OrderBy(s => s.Order)
            .Select(s => new StepDto(
                s.Id,
                s.Order,
                s.Title,
                s.Role))
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
        
        if (process.Kind == ProcessKind.FreeDocument)
            return Result<Guid>.Failure("WRONG_PROCESS_KIND", "Memorandums and letters do not use templates.");
        
        var hasPending = await context.Submissions.AnyAsync(
            submission => 
                submission.ProcessId == processId && 
                !submission.IsDeleted && 
                submission.Status != Status.Approve &&
                submission.Status != Status.Reject &&
                submission.Status != Status.Cancel, cancellationToken);
        
        if (hasPending)
            return Result<Guid>.Failure("PROCESS_HAS_PENDING_SUBMISSIONS", "Template cannot be replaced while submissions are pending.");
        
        var oldSteps = process.Steps.ToList();
        var oldFields = process.Fields.ToList();

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
            $"{document.Id}/{Path.GetFileName(request.FileName)}",
            "application/octet-stream",
            Prefix.Template,
            cancellationToken);

        document.Link = storageKey;

        var fallback = 1000;

        var newFields = new List<Field>();
        var newSteps = new List<Step>();

        foreach (var acroField in extractionResult.Data!)
        {
            if (acroField.IsSignature)
            {
                if (int.TryParse(acroField.Name.Split(':')[0], out var stepNum))
                {
                    if (stepNum > 0)
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
                        };

                        context.Steps.Add(step);
                        newSteps.Add(step);
                    }
                    else
                    {
                        var userSignatureField = new Field
                        {
                            Id = Guid.NewGuid(),
                            ProcessId = processId,
                            Title = acroField.Title,
                            AcroFormKey = acroField.Name,
                            Type = InputTypes.Signature,
                            Options = null,
                            Required = false,
                            DocumentId = document.Id,
                            Order = fallback++
                        };
                    
                        context.Fields.Add(userSignatureField);
                        newFields.Add(userSignatureField);
                    }
                }
                
                continue;
            }
            
            var order = int.TryParse(acroField.Name.Split('-')[0].Trim(), out var n) ? n : fallback++;

            var field = new Field
            {
                Id = Guid.NewGuid(),
                ProcessId = processId,
                Title = acroField.Title,
                AcroFormKey = acroField.Name,
                Type = acroField.Type,
                Options = acroField.Options,
                Required = acroField.Type != InputTypes.Checkbox,
                DocumentId = document.Id,
                Order = order,
            };

            context.Fields.Add(field);
            newFields.Add(field);
        }
        
        var oldFieldKeys = oldFields
            .ToDictionary(
            f => f.Id, 
            f => f.AcroFormKey);
        
        var newFieldByKey = newFields
            .GroupBy(
                f => 
                    f.AcroFormKey)
            .ToDictionary(
                g => g.Key, 
                g => g.First());
        
        var oldStepKey = oldSteps
            .ToDictionary(
                s => s.Id, 
                s => (s.Order, s.Title));
        
        var newStepByKey = newSteps
                .GroupBy(
                    s => (s.Order, s.Title))
                .ToDictionary(
                    g => g.Key, 
                    g => g.First());
        
        var inputs = await context.Inputs
            .Where(i => i.Submission.ProcessId == processId)
            .ToListAsync(cancellationToken);
        
        foreach (var input in inputs.Where(i => oldFieldKeys.ContainsKey(i.FieldId)))
            if (newFieldByKey.TryGetValue(oldFieldKeys[input.FieldId], out var newField))
                input.FieldId = newField.Id;
        
        var responses = await context.Responses
            .Where(i => i.Submission.ProcessId == processId)
            .ToListAsync(cancellationToken);

        foreach (var response in responses.Where(r => oldStepKey.ContainsKey(r.StepId)))
            if (newStepByKey.TryGetValue(oldStepKey[response.StepId], out var newStep))
                response.StepId = newStep.Id;
        
        var submissions = await context.Submissions
            .Where(s => s.ProcessId == processId)
            .ToListAsync(cancellationToken);
        
        foreach (var submission in submissions.Where(s => oldStepKey.ContainsKey(s.StepId)))
            if (newStepByKey.TryGetValue(oldStepKey[submission.StepId], out var newStep))
                submission.StepId = newStep.Id;

        await context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(document.Id);
    }

    public async Task<Result<(Stream Content, string FileName)>> DownloadDocumentTemplateAsync(Guid processId, CancellationToken cancellationToken = default)
    {
        if (currentUserService.UserId is null)
            return Result<(Stream Content, string FileName)>.Failure("USER_NOT_FOUND", "User not found.");

        if (processId == Guid.Empty)
            return Result<(Stream Content, string FileName)>.Failure("INVALID_PROCESS_ID", "Invalid process id");
        
        var process = await context.Processes
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(p => p.Id == processId, cancellationToken);

        if (process is null)
            return Result<(Stream Content, string FileName)>.Failure("PROCESS_NOT_FOUND", "Process not found.");
        
        var document = process.Documents.FirstOrDefault(d => !d.IsDeleted);
        
        if (document is null || string.IsNullOrEmpty(document.Link))
            return Result<(Stream Content, string FileName)>.Failure("DOCUMENT_NOT_FOUND", "No template for this process.");

        try
        {
            var content = await storageService.DownloadAsync(document.Link, cancellationToken);
            return Result<(Stream Content, string FileName)>.Success((content, Path.GetFileName(document.Link)));
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            // TODO: Log the error
            return Result<(Stream Content, string FileName)>.Failure("DOCUMENT_NOT_FOUND", "No template for this process.");
        }
    }
}