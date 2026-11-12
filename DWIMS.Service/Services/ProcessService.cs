using DWIMS.Data;
using DWIMS.Service.Analysis;
using DWIMS.Service.Common;
using DWIMS.Service.Logs;
using DWIMS.Service.Process;
using DWIMS.Service.Process.Dtos;
using DWIMS.Service.Process.Requests;
using DWIMS.Service.Storage;
using DWIMS.Service.User;
using iText.Kernel.Pdf.Event;
using Microsoft.EntityFrameworkCore;

namespace DWIMS.Service.Services;

public class ProcessService(
    AppDbContext context, 
    ICurrentUserService currentUserService, 
    ITemplateAnalysisEngine templateAnalysisEngine,
    IStorageService storageService, 
    IAcroFormService acroFormService,
    ILogService logService) : IProcessService
{
    private static readonly System.Text.RegularExpressions.Regex ControlNumberPattern = 
        new("^[A-Z0-9]+(-[A-Z0-9]+)+$", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static Result<string?> NormalizeControlNumber(string? raw)
    {
        var value = raw?.Trim().ToUpperInvariant();
        
        if (string.IsNullOrEmpty(value))
            return Result<string?>.Success(null);
        
        if (value.Length > 64)
            return Result<string?>.Failure("INVALID_CONTROL_NUMBER", "Control number must be 64 characters or fewer.");
        
        if (!ControlNumberPattern.IsMatch(value))
            return Result<string?>.Failure("INVALID_CONTROL_NUMBER", "Control number must be letters/digits in hyphen-separated segments. e.g. KLD-HRD-FR-0016-00.");

        return Result<string?>.Success(value);
    }

    private static Result<(string? Pattern, string? Message)> NormalizePattern(string? raw, string? rawMessage)
    {
        var value = raw?.Trim();
        
        if (string.IsNullOrEmpty(value))
            return Result<(string? Pattern, string? Message)>.Success((null, null));
        
        if (value.Length > 256)
            return Result<(string? Pattern, string? Message)>.Failure("INVALID_PATTERN", "Pattern must be 256 characters or fewer.");
        
        var error = FieldPatterns.Validate(value);

        if (error is not null)
            return Result<(string? Pattern, string? Message)>.Failure("INVALID_PATTERN", $"Invalid regular expression: {error}");
        
        var message = string.IsNullOrWhiteSpace(rawMessage) ? null : rawMessage.Trim();
        if (message?.Length > 200)
            return Result<(string? Pattern, string? Message)>.Failure("INVALID_PATTERN_MESSAGE", "Pattern message must be 200 characters or fewer.");

        return Result<(string? Pattern, string? Message)>.Success((value, message));
    }
    
    public async Task<Result<IReadOnlyList<ProcessSummaryDto>>> GetProcessesAsync(CancellationToken cancellationToken = default)
    {
        var processes = await context.Processes
            .Where(p => !p.IsDeleted && p.IsActive)
            .Select(p => new ProcessSummaryDto(
                p.Id,
                p.Title,
                p.Description,
                p.Steps.Count,
                context.Documents.Any(d => d.ProcessId == p.Id && !d.IsDeleted),
                p.Kind,
                p.IsActive,
                p.ControlNumber
            ))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ProcessSummaryDto>>.Success(processes);
    }

    public async Task<Result<PagedResult<ProcessSummaryDto>>> GetAllProcessesAsync(
        int page = 1,
        int pageSize = 100,
        string? query = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var term = query?.Trim();

        var baseQuery = context.Processes.Where(p => !p.IsDeleted);

        if (!string.IsNullOrEmpty(term))
        {
            baseQuery = baseQuery
                .Where(p => 
                    p.Title.Contains(term) || 
                    (p.Description != null && p.Description.Contains(term)) ||
                    (p.ControlNumber != null && p.ControlNumber.Contains(term)));
        }
        
        var totalCount = await baseQuery.CountAsync(cancellationToken);
        
        var processes = await baseQuery
            .OrderBy(p => p.Title)
            .ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProcessSummaryDto(
                p.Id,
                p.Title,
                p.Description,
                p.Steps.Count,
                context.Documents.Any(d => d.ProcessId == p.Id && !d.IsDeleted),
                p.Kind,
                p.IsActive,
                p.ControlNumber
            ))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<ProcessSummaryDto>>.Success(new PagedResult<ProcessSummaryDto>(processes, totalCount, page, pageSize));
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
                p.Description,
                p.Kind,
                p.Steps.Where(s => !s.IsDeleted).Select(s => new StepDto(
                    s.Id,
                    s.Order,
                    s.Title,
                    s.Role,
                    s.SignaturePage,
                    s.SignatureX,
                    s.SignatureY,
                    s.SignatureWidth,
                    s.SignatureHeight)).ToList(),
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
                    f.Order,
                    f.Pattern,
                    f.PatternMessage,
                    f.RectPage,
                    f.RectX,
                    f.RectY,
                    f.RectWidth,
                    f.RectHeight)).ToList(),
                p.ControlNumber,
                p.Documents.Any(d => !d.IsDeleted && d.Link != "")
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (process is null)
            return Result<ProcessDetailDto>.Failure("PROCESS_NOT_FOUND", "Process not found.");

        return Result<ProcessDetailDto>.Success(process);
    }

    public async Task<Result<Guid>> CreateProcessAsync(CreateProcessRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(request.Kind)) return Result<Guid>.Failure("INVALID_PROCESS_KIND", "Invalid process kind.");
        
        var control = NormalizeControlNumber(request.ControlNumber);
        
        if (!control.IsSuccess) 
            return Result<Guid>.Failure(control.Error!, control.ErrorDescription!);
        
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result<Guid>.Failure("INVALID_TITLE", "Title is required.");
        
        if (request.Title.Length > 200)
            return Result<Guid>.Failure("TITLE_TOO_LONG", "Title is too long.");
        
        if (request.Description?.Length > 2000)
            return Result<Guid>.Failure("DESCRIPTION_TOO_LONG", "Description is too long.");

        var process = new Data.Process
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Kind = request.Kind,
            ControlNumber = control.Data,
            Description = request.Description?.Trim()
        };
        
        context.Processes.Add(process);
        
        await context.SaveChangesAsync(cancellationToken);
        
        return Result<Guid>.Success(process.Id);
    }

    public async Task<Result> SetControlNumberAsync(
        Guid processId,
        SetControlNumberRequest request,
        CancellationToken cancellationToken = default)
    {
        var process = await context.Processes.FirstOrDefaultAsync(p => p.Id == processId, cancellationToken);
        
        if (process is null)
            return Result.Failure("PROCESS_NOT_FOUND", "Process not found.");
        
        var control = NormalizeControlNumber(request.ControlNumber);
        
        if (!control.IsSuccess)
            return Result.Failure(control.Error!, control.ErrorDescription!);

        process.ControlNumber = control.Data;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> UpdateProcessAsync(Guid id, UpdateProcessRequest request, CancellationToken cancellationToken = default)
    {
        var process = await context.Processes.Where(p => p.Id == id).FirstOrDefaultAsync(cancellationToken);
        
        if (process is null)
            return Result.Failure("PROCESS_NOT_FOUND", "Process not found.");
        
        if (request.Description?.Length > 2000)
            return Result.Failure("DESCRIPTION_TOO_LONG", "Description is too long.");
        
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result.Failure("INVALID_TITLE", "Title is required.");
        
        if (request.Title.Length > 200)
            return Result.Failure("TITLE_TOO_LONG", "Title is too long.");
        
        process.Description = request.Description?.Trim();

        process.Title = request.Title.Trim();
        

        await context.SaveChangesAsync(cancellationToken);
        
        await logService.LogAsync("Process Updated", "Process", process.Id, cancellationToken: cancellationToken);

        return Result.Success();
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
        
        if (submissions.Any())
            return Result.Failure("PROCESS_HAS_SUBMISSIONS", "Process has submissions, please consider retiring instead.");

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

    public async Task<Result> RetireProcessAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var process = await context.Processes
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        
        if (process is null)
            return Result.Failure("PROCESS_NOT_FOUND", "Process not found.");
        
        if (!process.IsActive)
            return Result.Failure("PROCESS_ALREADY_INACTIVE", "Process is already inactive.");
        
        process.IsActive = false;

        await context.SaveChangesAsync(cancellationToken);
        
        await logService.LogAsync("Process Retired", "Process", process.Id, cancellationToken: cancellationToken);

        return Result.Success();
    }

    public async Task<Result> RestoreProcessAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var process = await context.Processes
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        
        if (process is null)
            return Result.Failure("PROCESS_NOT_FOUND", "Process not found.");
        
        if (process.IsActive)
            return Result.Failure("PROCESS_ALREADY_ACTIVE", "Process is already active.");
        
        process.IsActive = true;

        await context.SaveChangesAsync(cancellationToken);
        
        await logService.LogAsync("Process Restored", "Process", process.Id, cancellationToken: cancellationToken);

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
        
        var normalized = NormalizePattern(request.Pattern, request.PatternMessage);
        
        if (!normalized.IsSuccess)
            return Result<Guid>.Failure(normalized.Error!, normalized.ErrorDescription!);

        var field = new Field
        {
            Id = Guid.NewGuid(),
            ProcessId = processId,
            Title = request.Title,
            Type = request.InputType,
            Required = request.Required,
            Order = await context.Fields.Where(f => f.ProcessId == processId).MaxAsync(f => (int?)f.Order) + 1 ?? 1,
            Pattern = normalized.Data.Pattern,
            PatternMessage = normalized.Data.Message
        };
        
        context.Fields.Add(field);
        
        await context.SaveChangesAsync(cancellationToken);
        
        return Result<Guid>.Success(field.Id);
    }

    public async Task<Result> UpdateFieldAsync(Guid processId, Guid fieldId, UpdateFieldRequest request,
        CancellationToken cancellationToken = default)
    {
        var field = await context.Fields
            .FirstOrDefaultAsync(f => f.Id == fieldId && f.ProcessId == processId && !f.IsDeleted, cancellationToken);
        
        if (field is null)
            return Result.Failure("FIELD_NOT_FOUND", "Field not found.");
        
        var normalized = NormalizePattern(request.Pattern, request.PatternMessage);
        
        if (!normalized.IsSuccess)
            return Result.Failure(normalized.Error!, normalized.ErrorDescription!);

        (field.Pattern, field.PatternMessage) = normalized.Data;

        await context.SaveChangesAsync(cancellationToken);

        await logService.LogAsync("Field Updated", "Field", field.Id, cancellationToken: cancellationToken);

        return Result.Success();
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
        
        var oldFieldByKey = oldFields
            .GroupBy(f => f.AcroFormKey)
            .ToDictionary(g => g.Key, g => g.First());

        context.Steps.RemoveRange(process.Steps);
        process.Fields.ToList().ForEach(f => context.Fields.Remove(f));

        using var ms = new MemoryStream();
        await request.File.CopyToAsync(ms, cancellationToken);
        ms.Position = 0;

        var extractionResult = await acroFormService.ExtractFieldsAsync(ms, cancellationToken);
        
        var isFlat = !extractionResult.IsSuccess && extractionResult.Error == "NOT_AN_ACROFORM";

        if (!extractionResult.IsSuccess && !isFlat)
            return Result<Guid>.Failure(extractionResult.Error!, extractionResult.ErrorDescription!);

        TemplateAnalysis? analysis = null;

        if (isFlat)
        {
            ms.Position = 0;
            var analysisResult = await templateAnalysisEngine.AnalyzeAsync(ms, cancellationToken);

            if (!analysisResult.IsSuccess)
                return Result<Guid>.Failure(analysisResult.Error!, analysisResult.ErrorDescription!);
            
            analysis = analysisResult.Data!;
        }

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

        if (analysis is not null)
        {
            document.MappingState = TemplateMappingState.Analyzed;
            document.AnalysisJson = TemplateAnalysisJson.Serialize(analysis);
        }
        else
        {
            document.MappingState = TemplateMappingState.NotAnalyzed;
            document.AnalysisJson = null;
        }

        var fallback = 1000;

        var newFields = new List<Field>();
        var newSteps = new List<Step>();

        IReadOnlyList<AcroFormField> acroFields =
            extractionResult.IsSuccess ? extractionResult.Data! : [];

        foreach (var acroField in acroFields)
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
            
            var previous = oldFieldByKey.TryGetValue(acroField.Name, out var old) ? old : null;

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
                Pattern = previous?.Pattern,
                PatternMessage = previous?.PatternMessage,
            };

            context.Fields.Add(field);
            newFields.Add(field);
        }

        await RelinkAndSaveAsync(processId, oldFields, oldSteps, newFields, newSteps, cancellationToken);

        if (analysis is not null)
            await logService.LogAsync(
                "Template Analyzed",
                "Document",
                document.Id,
                $"{analysis.Fields.Count} field and {analysis.Steps.Count} step suggestions.",
                cancellationToken: cancellationToken);
            
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

    public async Task<Result<TemplateMappingDto>> GetMappingAsync(Guid processId, CancellationToken cancellationToken = default)
    {
        var process = await context.Processes
            .Include(p => p.Steps)
            .Include(p => p.Fields)
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(p => p.Id == processId, cancellationToken);
        
        if (process is null) 
            return Result<TemplateMappingDto>.Failure("PROCESS_NOT_FOUND", "Process not found.");
        
        var document = process.Documents.FirstOrDefault(d => !d.IsDeleted);
        
        if (document is null || document.MappingState == TemplateMappingState.NotAnalyzed)
            return Result<TemplateMappingDto>.Failure("MAPPING_NOT_FOUND", "No template analysis for this process.");

        TemplateAnalysis? suggestions = null;
        IReadOnlyList<FieldDto> fields = [];
        IReadOnlyList<StepDto> steps = [];

        if (document.MappingState == TemplateMappingState.Analyzed)
        {
            suggestions = TemplateAnalysisJson.Deserialize(document.AnalysisJson ?? "") ??
                          new TemplateAnalysis(0, [], [], []);
        }
        else
        {
            steps = process.Steps
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.Order)
                .Select(s => new StepDto(
                    s.Id,
                    s.Order,
                    s.Title,
                    s.Role,
                    s.SignaturePage,
                    s.SignatureX,
                    s.SignatureY,
                    s.SignatureWidth,
                    s.SignatureHeight))
                .ToList();
            
            fields = process.Fields
                .Where(f => !f.IsDeleted)
                .OrderBy(f => f.Order)
                .ThenBy(f => f.Title)
                .Select(f => new FieldDto(
                    f.Id,
                    f.Title,
                    f.AcroFormKey,
                    f.Type,
                    f.Required,
                    f.Options,
                    f.Order,
                    f.Pattern,
                    f.PatternMessage,
                    f.RectPage,
                    f.RectX,
                    f.RectY,
                    f.RectWidth,
                    f.RectHeight))
                .ToList();
        }

        return Result<TemplateMappingDto>.Success(
            new TemplateMappingDto((int)document.MappingState, suggestions, fields, steps));
    }

    public async Task<Result> ConfirmMappingAsync(Guid processId, ConfirmMappingRequest request, CancellationToken cancellationToken = default)
    {
        var process = await context.Processes
            .Include(p => p.Steps)
            .Include(p =>  p.Fields)
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(p => p.Id == processId, cancellationToken);
        
        if (process is null)
            return Result.Failure("PROCESS_NOT_FOUND", "Process not found.");

        if (process.Kind == ProcessKind.FreeDocument)
            return Result.Failure("WRONG_PROCESS_KIND", "Memorandums and letters do not use templates.");
        
        var document = process.Documents.FirstOrDefault(d => d.ProcessId == processId && !d.IsDeleted);

        if (document is null || document.MappingState == TemplateMappingState.NotAnalyzed)
            return Result.Failure("MAPPING_NOT_REQUIRED", "This template was not analyzed; nothing to confirm.");

        var hasPending = await context.Submissions
            .AnyAsync(submission =>
                submission.ProcessId == processId &&
                !submission.IsDeleted &&
                submission.Status != Status.Approve &&
                submission.Status != Status.Reject &&
                submission.Status != Status.Cancel, cancellationToken);

        if (hasPending)
            return Result.Failure("PROCESS_HAS_PENDING_SUBMISSIONS",
                "Mapping cannot be replaced while submissions are pending.");

        var pageCount = TemplateAnalysisJson.Deserialize(document.AnalysisJson ?? "")?.PageCount ?? 0;

        var fieldPatterns = new List<(string? Pattern, string? Message)>();

        foreach (var item in request.Fields)
        {
            var title = item.Title?.Trim() ?? "";
            
            if (title.Length is < 1 or > 200)
                return Result.Failure("INVALID_TITLE", "Every field needs a title of 1-200 characters.");

            if (!Enum.IsDefined((InputTypes)item.Type))
                return Result.Failure("INVALID_FIELD_TYPE", $"Unknown field type {item.Type}.");

            if (!IsValidPlacement(item.Page, item.X, item.Y, item.Width, item.Height, pageCount))
                return Result.Failure("INVALID_FIELD_PLACEMENT", $"Field '{title}' has an invalid placement.");

            var normalized = NormalizePattern(item.Pattern, item.PatternMessage);

            if (!normalized.IsSuccess)
                return Result.Failure(normalized.Error!, normalized.ErrorDescription!);
            
            fieldPatterns.Add(normalized.Data);
        }
        
        foreach (var item in request.Steps)
        {
            var title = item.Title?.Trim() ?? "";
            
            if (title.Length is < 1 or > 200)
                return Result.Failure("INVALID_TITLE", "Every step needs a title of 1-200 characters.");

            if (!Enum.IsDefined((GeneralRole)item.Role))
                return Result.Failure("INVALID_ROLE", $"Unknown role {item.Role}.");

            if (!IsValidPlacement(item.Page, item.X, item.Y, item.Width, item.Height, pageCount))
                return Result.Failure("MAPPING_INCOMPLETE", $"Step '{title}' needs a valid signature slot.");
        }

        var oldFields = process.Fields.ToList();
        var oldSteps = process.Steps.ToList();
        
        context.Steps.RemoveRange(process.Steps);
        process.Fields.ToList().ForEach(f => context.Fields.Remove(f));

        var newFields = new List<Field>();

        for (int i = 0; i < request.Fields.Count; i++)
        {
            var item = request.Fields[i];
            
            newFields.Add(new Field
            {
                Id = Guid.NewGuid(),
                ProcessId = processId,
                Title = item.Title.Trim(),
                AcroFormKey = null,
                Type = (InputTypes) item.Type,
                Required = item.Required,
                RectPage = item.Page,
                RectX = item.X,
                RectY = item.Y,
                RectWidth = item.Width,
                RectHeight = item.Height,
                Pattern = fieldPatterns[i].Pattern,
                PatternMessage = fieldPatterns[i].Message,
                DocumentId = document.Id,
                Order = i + 1,
            });
        }
        
        var newSteps = new List<Step>();

        for (int i = 0; i < request.Steps.Count; i++)
        {
            var item = request.Steps[i];
            
            newSteps.Add(new Step
            {
                Id = Guid.NewGuid(),
                ProcessId = processId,
                Order = i + 1,
                Title = item.Title.Trim(),
                Role = (GeneralRole) item.Role,
                SignaturePage = item.Page,
                SignatureX = item.X,
                SignatureY = item.Y,
                SignatureWidth = item.Width,
                SignatureHeight = item.Height
            });
        }
        
        context.Fields.AddRange(newFields);
        context.Steps.AddRange(newSteps);
        
        document.MappingState = TemplateMappingState.Confirmed;

        await RelinkAndSaveAsync(processId, oldFields, oldSteps, newFields, newSteps, cancellationToken);
        
        await logService.LogAsync(
            "Template Mapping Confirmed",
            "Process",
            processId,
            $"{newFields.Count} fields, {newSteps.Count} steps",
            cancellationToken: cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ReanalyzeAsync(Guid processId, CancellationToken cancellationToken = default)
    {
        var process = await context.Processes
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(p => p.Id == processId, cancellationToken);
        
        if (process is null)
            return Result.Failure("PROCESS_NOT_FOUND", "Process not found.");
        
        var document = process.Documents.FirstOrDefault(d => d.ProcessId == processId && !d.IsDeleted);

        if (document is null || document.MappingState == TemplateMappingState.NotAnalyzed)
            return Result.Failure("MAPPING_NOT_REQUIRED", "This template was not analyzed.");

        if (document.MappingState == TemplateMappingState.Confirmed)
            return Result.Failure("MAPPING_ALREADY_CONFIRMED", "Re-upload the template to redo a confirmed mapping.");

        Stream template;

        try
        {
            template = await storageService.DownloadAsync(document.Link, cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            return Result.Failure("DOCUMENT_NOT_FOUND", "Template file could not be loaded.");
        }

        var analysisResult = await templateAnalysisEngine.AnalyzeAsync(template, cancellationToken);

        if (!analysisResult.IsSuccess)
            return Result.Failure(analysisResult.Error!, analysisResult.ErrorDescription!);

        document.AnalysisJson = TemplateAnalysisJson.Serialize(analysisResult.Data!);
        
        await context.SaveChangesAsync(cancellationToken);

        await logService.LogAsync(
            "Template Reanalyzed",
            "Document",
            document.Id,
            $"{analysisResult.Data!.Fields.Count} field and {analysisResult.Data.Steps.Count} step suggestions.",
            cancellationToken: cancellationToken);

        return Result.Success();
    }

    private static bool IsValidPlacement(int page, double x, double y, double width, double height, int pageCount) =>
        page >= 0 && (pageCount == 0 || page < pageCount) &&
        x is >= 0 and <= 1 && y is >= 0 and <= 1 &&
        width is > 0 and <= 1 && height is > 0 and <= 1;

    private async Task RelinkAndSaveAsync(
        Guid processId,
        IReadOnlyList<Field> oldFields,
        IReadOnlyList<Step> oldSteps,
        IReadOnlyList<Field> newFields,
        IReadOnlyList<Step> newSteps,
        CancellationToken cancellationToken)
    {
        var oldFieldKeys = oldFields
            .ToDictionary(
            f => f.Id, 
            f => FieldMatchKey(f));
        
        var newFieldByKey = newFields
            .GroupBy(FieldMatchKey)
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
    }

    private static string FieldMatchKey(Field field) =>
        field.AcroFormKey is { } key ? $"acro:{key}"
        : field.RectPage is { } page ? $"rect:{field.Title}|{page}|{field.RectX:F3}|{field.RectY:F3}"
        : $"manual:{field.Title}|{field.Order}";
}