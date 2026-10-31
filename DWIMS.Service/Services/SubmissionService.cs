using System.Security.Cryptography;
using System.Text.Json;
using DWIMS.Data;
using DWIMS.Service.Common;
using DWIMS.Service.Common.Models;
using DWIMS.Service.Logs;
using DWIMS.Service.Process.Dtos;
using DWIMS.Service.Process.Requests;
using DWIMS.Service.Routing;
using DWIMS.Service.Routing.Dtos;
using DWIMS.Service.Storage;
using DWIMS.Service.Submission;
using DWIMS.Service.Submission.Dtos;
using DWIMS.Service.Submission.Requests;
using DWIMS.Service.User;
using iText.Kernel.Pdf;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace DWIMS.Service.Services;

public class SubmissionService(
    AppDbContext context, 
    ICurrentUserService currentUser,
    EmailQueue emailQueue,
    IRoutingService routingService,
    IPdfGenerationService pdfGenerationService,
    IStorageService storageService,
    ILogService logService) 
    : ISubmissionService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    
    private const string CodeAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ23456789";
    
    private record CheckBoxOptions(string On);

    private record FieldOptions(string Value, string Label);
    
    public static string GenerateVerificationCode(int length = 8)
    {
        return string.Create(length, 0,
            (chars, _) =>
            {
                for (var i = 0; i < chars.Length; i++)
                    chars[i] = CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)];
            });
    }
    
    private async Task<string> GenerateVerificationCodeAsync(Data.Process process, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(process.ControlNumber))
        {
            var legacy = GenerateVerificationCode();
            while (await context.Submissions.AnyAsync(s => s.VerificationCode == legacy, cancellationToken))
                legacy = GenerateVerificationCode();

            return legacy;
        }
        
        var baseNumber = process.ControlNumber.Trim();
        if (baseNumber.StartsWith("KLD-", StringComparison.OrdinalIgnoreCase))
            baseNumber = baseNumber[4..];

        var serial = process.LastIssuedSerial + 1;
        var code = $"{baseNumber}-{serial:D4}";

        while (await context.Submissions.AnyAsync(s => s.VerificationCode == code, cancellationToken))
        {
            serial++;
            code = $"{baseNumber}-{serial:D4}";
        }
        
        process.LastIssuedSerial = serial;

        return code;
    }

    private async Task<Result<IEnumerable<SubmissionRoute>>> ValidateRouteAsync(IReadOnlyList<RouteEntryDto> route, IReadOnlyList<SlotEntryDto> slots, Guid submissionId, int pages, CancellationToken cancellationToken)
    {
        IEnumerable<SubmissionRoute> routes = new List<SubmissionRoute>();
        
        foreach (var step in route)
        {
            var slot = slots.FirstOrDefault(s => s.stepId == step.StepId);
            
            if (slot is null)
                return Result<IEnumerable<SubmissionRoute>>.Failure("INVALID_SLOT", $"No signature slot found for step {step.StepId}.");
            
            if (pages < slot.Page || slot.Page < 1)
                return Result<IEnumerable<SubmissionRoute>>.Failure("INVALID_SLOT", $"Page {slot.Page} for step {step.StepId} is invalid.");
            
            if (0 > slot.SignatureX || 
                0 > slot.SignatureY || 
                0 >= slot.SignatureWidth || 
                0 >= slot.SignatureHeight ||
                slot.SignatureWidth + slot.SignatureX > 1.02 ||
                slot.SignatureHeight + slot.SignatureY > 1.02)
                return Result<IEnumerable<SubmissionRoute>>.Failure("INVALID_SLOT", $"Signature slot dimensions for step {step.StepId} is invalid.");
            
            if (string.IsNullOrWhiteSpace(step.AssigneeEmail))
                return Result<IEnumerable<SubmissionRoute>>.Failure("INVALID_REVIEWER", $"Assignee email for step {step.StepId} is empty.");
            
            var assignee = await context.Users.FirstOrDefaultAsync(u => u.Email == step.AssigneeEmail, cancellationToken: cancellationToken);
            
            if (assignee is null)
                return Result<IEnumerable<SubmissionRoute>>.Failure("INVALID_REVIEWER", $"Assignee email for step {step.StepId} does not exist.");
            
            if (assignee.Id == currentUser.UserId)
                return Result<IEnumerable<SubmissionRoute>>.Failure("FORBIDDEN", $"Assignee email for step {step.StepId} cannot be the current user.");
            
            var submissionRoute = new SubmissionRoute
            {
                Id = Guid.NewGuid(),
                StepId = step.StepId,
                AssigneeId = assignee.Id,
                SubmissionId = submissionId,
                SignatureHeight = slot.SignatureHeight,
                SignatureWidth = slot.SignatureWidth,
                SignatureX = slot.SignatureX,
                SignatureY = slot.SignatureY,
                SignaturePage = slot.Page
            };
            
            routes = routes.Append(submissionRoute);
            
            context.SubmissionRoutes.Add(submissionRoute);
        }
        
        return Result<IEnumerable<SubmissionRoute>>.Success(routes);
    }
    
    public async Task<Result<SubmissionsPageDto>> GetMySubmissionsAsync(Status? statusFilter, Guid? processId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Result<SubmissionsPageDto>.Failure("UNAUTHORIZED", "User is not authenticated.");
        
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = context.Submissions
            .Where(s => s.SubmitterId == currentUser.UserId.Value);

        if (statusFilter.HasValue)
            query = query.Where(s => s.Status == statusFilter.Value);
        
        var categories = (await query
            .GroupBy(s => new { s.ProcessId, s.Process.Title })
            .Select(g => new SubmissionsCategoryDto(g.Key.ProcessId, g.Key.Title, g.Count()))
            .ToListAsync(cancellationToken))
            .OrderByDescending(c => c.Count)
            .ToList();
        
        if (processId.HasValue)
            query = query.Where(s => s.ProcessId == processId.Value);
        
        var totalCount = await query.CountAsync(cancellationToken);

        var submissions = await query
            .OrderByDescending(s => s.SubmittedOn)
            .ThenBy(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SubmissionSummaryDto(
                s.Id,
                s.Process.Title,
                s.Status,
                s.SubmittedOn,
                s.Step.Title,
                s.ProcessId
            ))
            .ToListAsync(cancellationToken);

        return Result<SubmissionsPageDto>.Success(new SubmissionsPageDto(submissions, categories, totalCount, page, pageSize));
    }

    public async Task<Result<PagedResult<AdminSubmissionDto>>> GetAllSubmissionsAsync(SubmissionFilterRequest filter, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Result<PagedResult<AdminSubmissionDto>>.Failure("UNAUTHORIZED", "User is not authenticated.");
        
        if (!currentUser.isSuperAdministrator)
            return Result<PagedResult<AdminSubmissionDto>>.Failure("UNAUTHORIZED", "Only super administrators are authorized to view all submissions.");
        
        var page = Math.Max(filter.Page, 1);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var query = context.Submissions
            .Where(s => !s.IsDeleted);

        if (filter.SubmitterId.HasValue)
            query = query.Where(s => s.SubmitterId == filter.SubmitterId);
        
        if (filter.AssigneeId.HasValue)
            query = query
                .Where(s => 
                    s.Responses.Any(r => r.AssigneeId == filter.AssigneeId && r.CompletedOn == null));
        
        if (filter.ProcessId.HasValue)
            query = query
                .Where(s => 
                    s.ProcessId == filter.ProcessId);
        
        if (filter.Status.HasValue)
            query = query
                .Where(s => 
                    s.Status == filter.Status);
        
        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query
                .Where(s => 
                    s.Process.Title.Contains(filter.Search) ||
                    s.Submitter.FirstName.Contains(filter.Search) ||
                    s.Submitter.LastName.Contains(filter.Search) ||
                    s.Step.Title.Contains(filter.Search));
        
        var totalCount = await query.CountAsync(cancellationToken);

        var submissions = await query
            .OrderByDescending(s => s.SubmittedOn)
            .ThenBy(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new AdminSubmissionDto(
                s.Id,
                s.VerificationCode,
                s.Process.Title,
                s.Status,
                s.SubmittedOn,
                s.CompletedOn,
                $"{s.Submitter.FirstName} {s.Submitter.LastName}",
                s.Step.Title,
                s.Responses
                    .Where(r => r.CompletedOn == null)
                    .Select(r => $"{r.Assignee.FirstName} {r.Assignee.LastName}")
                    .FirstOrDefault()
            ))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<AdminSubmissionDto>>.Success(new PagedResult<AdminSubmissionDto>(submissions, totalCount, page, pageSize));
    }

    public async Task<Result<PagedResult<PendingReviewDto>>> GetPendingReviewAsync(
        int page, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Result<PagedResult<PendingReviewDto>>.Failure("UNAUTHORIZED", "User is not authenticated.");
        
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = context.Submissions
            .Include(s => s.Step)
            .Include(s => s.Submitter)
            .Include(s => s.Process)
            .Include(s => s.Responses)
            .Where(s => s.Status == Status.Review || s.Status == Status.OnHold);

        query = query.Where(s => s.SubmitterId != currentUser.UserId.Value);

        query = query.Where(
            submission => 
                submission.Responses.Any(
                    response => 
                        response.StepId == submission.StepId && 
                        response.AssigneeId == currentUser.UserId));
        
        var totalCount = await query.CountAsync(cancellationToken);

        var pending = await query
            .OrderByDescending(s => s.SubmittedOn)
            .ThenBy(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new PendingReviewDto(
                s.Id,
                s.Process.Id,
                s.Process.Title,
                s.Step.Title,
                $"{s.Submitter.FirstName} {s.Submitter.LastName}",
                s.Status,
                s.SubmittedOn,
                s.CompletedOn
            ))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<PendingReviewDto>>.Success(new PagedResult<PendingReviewDto>(pending, totalCount, page, pageSize));
    }

    public async Task<Result<SubmissionDetailDto>> GetPendingReviewAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Result<SubmissionDetailDto>.Failure("UNAUTHORIZED", "User is not authenticated.");

        var submission = await context.Submissions
            .Include(s => s.Process)
            .Include(s => s.Step)
            .Include(s => s.Submitter)
            .Include(s => s.Inputs).ThenInclude(i => i.Field)
            .Include(s => s.Responses).ThenInclude(r => r.Reviewer)
            .Include(s => s.Responses).ThenInclude(r => r.Step)
            .Include(s => s.Attachments)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (submission is null)
            return Result<SubmissionDetailDto>.Failure("SUBMISSION_NOT_FOUND", "Submission not found.");
       
        if (currentUser.UserId != submission.SubmitterId && 
            !currentUser.isSuperAdministrator &&
            !submission.Responses.Any(r => r.Reviewer?.Id == currentUser.UserId || r.AssigneeId == currentUser.UserId))
            return Result<SubmissionDetailDto>.Failure("UNAUTHORIZED", "You are not authorized to access this submission.");

        var stepResponses = submission.Responses.Select(r => new SubmissionStepResponseDto(
            r.Id,
            r.Step.Title,
            r.Result?.ToString(),
            r.Remarks,
            r.Reviewer is not null ? $"{r.Reviewer.FirstName} {r.Reviewer.LastName}" : null,
            r.ActivatedOn,
            r.CompletedOn
        )).ToList();

        var fieldValues = submission.Inputs
            .OrderBy(i => i.Field.Order)
            .ThenBy(i => i.Field.Title)
            .Select(i => new SubmissionFieldValueDto(
            i.Field.Title,
            i.Field.Type.ToString(),
            i.Value
        )).ToList();

        var attachments = submission.Attachments.Where(
            attachment => !attachment.IsDeleted).ToList();
        var attachmentDtos = attachments.Select(
            attachment => new AttachmentDto(
                attachment.Id, 
                attachment.Title, 
                attachment.Type))
            .ToList();

        var route = await context.SubmissionRoutes
            .Include(r => r.Step)
            .Include(r => r.Assignee)
            .Where(r => r.SubmissionId == id)
            .OrderBy(r => r.Step.Order)
            .ToListAsync(cancellationToken);
        
        var routes = route.Select(r => new SubmissionRouteDto(
            r.StepId,
            r.Step.Title,
            r.Assignee?.Email,
            r.Assignee is null ? null : $"{r.Assignee.FirstName} {r.Assignee.LastName}"
        )).ToList();

        return Result<SubmissionDetailDto>.Success(new SubmissionDetailDto(
            submission.Id,
            submission.ProcessId,
            submission.Process.Title,
            submission.Status,
            submission.SubmittedOn,
            submission.CompletedOn,
            submission.Step.Title,
            submission.Process.Kind,
            submission.SourceTitle,
            stepResponses,
            fieldValues,
            attachmentDtos,
            routes,
            submission.VerificationCode
        ));
    }

    public async Task<Result<SubmissionDetailDto>> GetSubmissionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Result<SubmissionDetailDto>.Failure("UNAUTHORIZED", "You are not authorized to access this submission.");
        
        var submission = await context.Submissions
            .Include(s => s.Process)
            .Include(s => s.Step)
            .Include(s => s.Submitter)
            .Include(s => s.Inputs).ThenInclude(i => i.Field)
            .Include(s => s.Responses).ThenInclude(r => r.Reviewer)
            .Include(s => s.Responses).ThenInclude(r => r.Step)
            .Include(s => s.Attachments)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (submission is null)
            return Result<SubmissionDetailDto>.Failure("SUBMISSION_NOT_FOUND", "Submission not found.");
       
        if (currentUser.UserId != submission.SubmitterId && 
            !currentUser.isSuperAdministrator &&
            !submission.Responses.Any(r => r.Reviewer?.Id == currentUser.UserId || r.AssigneeId == currentUser.UserId))
            return Result<SubmissionDetailDto>.Failure("UNAUTHORIZED", "You are not authorized to access this submission.");

        var stepResponses = submission.Responses.Select(r => new SubmissionStepResponseDto(
            r.Id,
            r.Step.Title,
            r.Result?.ToString(),
            r.Remarks,
            r.Reviewer is not null ? $"{r.Reviewer.FirstName} {r.Reviewer.LastName}" : null,
            r.ActivatedOn,
            r.CompletedOn
        )).ToList();

        var fieldValues = submission.Inputs
            .OrderBy(f => f.Field.Order)
            .ThenBy(f => f.Field.Title)
            .Select(f => new SubmissionFieldValueDto(
            f.Field.Title,
            f.Field.Type.ToString(),
            f.Value
        )).ToList();

        var attachments = submission.Attachments.Where(
            attachment => !attachment.IsDeleted).ToList();
        var attachmentDtos = attachments.Select(
            attachment => new AttachmentDto(
                attachment.Id, 
                attachment.Title, 
                attachment.Type))
            .ToList();
        
        var route = await context.SubmissionRoutes
            .Include(r => r.Step)
            .Include(r => r.Assignee)
            .Where(r => r.SubmissionId == id)
            .OrderBy(r => r.Step.Order)
            .ToListAsync(cancellationToken);
        
        var routes = route.Select(r => new SubmissionRouteDto(
            r.StepId,
            r.Step.Title,
            r.Assignee?.Email,
            r.Assignee is null ? null : $"{r.Assignee.FirstName} {r.Assignee.LastName}"
        )).ToList();

        return Result<SubmissionDetailDto>.Success(new SubmissionDetailDto(
            submission.Id,
            submission.ProcessId,
            submission.Process.Title,
            submission.Status,
            submission.SubmittedOn,
            submission.CompletedOn,
            submission.Step.Title,
            submission.Process.Kind,
            submission.SourceTitle,
            stepResponses,
            fieldValues,
            attachmentDtos,
            routes,
            submission.VerificationCode
        ));
    }

    public async Task<Result<Guid>> CreateSubmissionAsync(CreateSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Result<Guid>.Failure("UNAUTHORIZED", "User is not authenticated.");

        var process = await context.Processes
            .Include(p => p.Steps.OrderBy(s => s.Order))
            .Include(p => p.Fields)
            .FirstOrDefaultAsync(p => p.Id == request.ProcessId, cancellationToken);

        if (process is null)
            return Result<Guid>.Failure("PROCESS_NOT_FOUND", "Process not found.");
        
        if (process.Kind == ProcessKind.FreeDocument)
            return Result<Guid>.Failure("WRONG_PROCESS_KIND", "Memorandums and letters must be submitted via the document submission endpoint.");
        
        if (!process.IsActive)
            return Result<Guid>.Failure("PROCESS_INACTIVE", "Process is inactive.");
        
        if (process.Fields.Any(f => !f.IsDeleted && f.Type == InputTypes.Signature) && 
            !await context.Signatures.AnyAsync(s => s.UserId == currentUser.UserId, cancellationToken))
            return Result<Guid>.Failure("SIGNATURE_REQUIRED", "User must register a signature in the settings before submitting.");
        
        var verificationCode = await GenerateVerificationCodeAsync(process, cancellationToken);
        
        var submission = new Data.Submission
        {
            Id = Guid.NewGuid(),
            VerificationCode = verificationCode,
            ProcessId = process.Id,
            SubmittedOn = DateTime.UtcNow,
            SubmitterId = currentUser.UserId.Value,
        };

        var requestSteps = request.Route.Select(r => r.StepId).ToHashSet();
        var processSteps = process.Steps.Where(s => !s.IsDeleted).Select(s => s.Id).ToHashSet();

        if (!requestSteps.SetEquals(processSteps))
            return Result<Guid>.Failure("INVALID_ROUTE", "Route does not match process steps.");

        IEnumerable<SubmissionRoute> routes = [];
        
        foreach (var step in request.Route)
        {
            if (string.IsNullOrWhiteSpace(step.AssigneeEmail))
                return Result<Guid>.Failure("INVALID_REVIEWER", $"Assignee email for step {step.StepId} is empty.");
            
            var assignee = await context.Users.FirstOrDefaultAsync(u => u.Email == step.AssigneeEmail, cancellationToken: cancellationToken);
            
            if (assignee is null)
                    return Result<Guid>.Failure("INVALID_REVIEWER", $"Assignee email for step {step.StepId} does not exist.");
            
            if (assignee.Id == currentUser.UserId)
                return Result<Guid>.Failure("FORBIDDEN", $"Assignee email for step {step.StepId} cannot be the current user.");
            
            var submissionRoute = new SubmissionRoute
            {
                Id = Guid.NewGuid(),
                StepId = step.StepId,
                AssigneeId = assignee.Id,
                SubmissionId = submission.Id,
            };
            
            routes = routes.Append(submissionRoute);
            
            context.SubmissionRoutes.Add(submissionRoute);
        }
        
        if (!processSteps.Any())
            return Result<Guid>.Failure("INVALID_PROCESS", "Process has no steps.");

        var firstStep = process.Steps.First(s => !s.IsDeleted);
        var start = await routingService.StartRoute(submission, process, routes.First(r => r.StepId == firstStep.Id));
        
        if (!start.IsSuccess)
            return Result<Guid>.Failure(start.Error!, start.ErrorDescription!);
        
        context.Submissions.Add(submission);

        foreach (var fieldInput in request.Fields)
        {
            var field = process.Fields.FirstOrDefault(f => f.Id == fieldInput.FieldId);
            
            if (field is null)
                return Result<Guid>.Failure("INVALID_FIELD", $"Field {fieldInput.FieldId} does not belong to this process.");
            
            if (field.Type == InputTypes.Signature)
                continue;
            
            var value = fieldInput.FieldValue ?? "";

            if (field.Type == InputTypes.Checkbox)
            {
                var on = field.Options is null ?
                    null : 
                    JsonSerializer.Deserialize<CheckBoxOptions>(field.Options, Json)?.On;

                if (value != "" && value != on)
                    return Result<Guid>.Failure("INVALID_FIELD_VALUE",
                        $"Invalid value for field {fieldInput.FieldId}");
            } else if (field.Type == InputTypes.Dropdown && field.Options is not null)
            {
                var allowed = JsonSerializer.Deserialize<List<FieldOptions>>(field.Options, Json)!
                    .Select(o => o.Value)
                    .ToHashSet();
                
                if (value != "" && !allowed.Contains(value))
                    return Result<Guid>.Failure("INVALID_FIELD_VALUE",
                        $"Invalid value for field {fieldInput.FieldId}");
            }
            
            if (!FieldPatterns.IsValid(field.Pattern, value))
                return Result<Guid>.Failure("INVALID_FIELD_VALUE",
                    field.PatternMessage ?? $"Field '{field.Title}' does not match the required format.");

            context.Inputs.Add(new Input
            {
                Id = Guid.NewGuid(),
                SubmissionId = submission.Id,
                FieldId = fieldInput.FieldId,
                Value = fieldInput.FieldValue ?? "",
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        await logService.LogAsync("Submission Created", "Submission", submission.Id);
        
        emailQueue.Enqueue(new EmailJob(EmailNotificationKind.SubmissionReceived, SubmissionId: submission.Id));
        
        return Result<Guid>.Success(submission.Id);
    }

    public async Task<Result> RespondToStepAsync(Guid submissionId, Guid stepId, RespondToStepRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Result.Failure("UNAUTHORIZED", "User is not authenticated.");
        
        if (request.Outcome is not Status.Approve and not Status.Reject and not Status.OnHold)
            return Result.Failure("INVALID_OUTCOME", "Outcome must be Approve or Reject.");
        
        if (request.Outcome is Status.OnHold or Status.Reject && string.IsNullOrWhiteSpace(request.Remarks))
            return Result.Failure("REASON_REQUIRED", "Remarks must be provided when outcome is Reject or OnHold.");

        var submission = await context.Submissions
            .Include(s => s.Process)
            .ThenInclude(p => p.Steps.OrderBy(st => st.Order))
            .Include(s => s.Responses)
            .FirstOrDefaultAsync(s => s.Id == submissionId, cancellationToken);

        if (submission is null)
            return Result.Failure("SUBMISSION_NOT_FOUND", "Submission not found.");

        if (submission.StepId != stepId)
            return Result.Failure("WRONG_STEP", "This submission is not on the specified step.");

        var currentStep = submission.Process.Steps.FirstOrDefault(s => s.Id == stepId);
        if (currentStep is null)
            return Result.Failure("STEP_NOT_FOUND", "Step not found.");

        var response = submission.Responses.FirstOrDefault(r => r.StepId == stepId && r.CompletedOn == null);
        
        if (response is null)
            return Result.Failure("RESPONSE_NOT_FOUND", "No active response found for this step.");
        
        if (response.AssigneeId != currentUser.UserId && !currentUser.isSuperAdministrator)
            return Result.Failure("FORBIDDEN", "User is not authorized to respond to this submission.");

        if (request.Outcome is Status.Approve)
        {
            var hasSignature = await context.Signatures.AnyAsync(s => s.UserId == currentUser.UserId, cancellationToken: cancellationToken);
            if (!hasSignature)
                return Result.Failure("SIGNATURE_REQUIRED", "You must register a signature in the settings before approving.");
        }

        if (request.Outcome is Status.OnHold)
        {
            response.Result = Status.OnHold;
            response.Remarks = request.Remarks ?? "";
            response.CompletedOn = DateTime.UtcNow;
            submission.Status = Status.OnHold;
            
            await context.SaveChangesAsync(cancellationToken);
            
            emailQueue.Enqueue(new EmailJob(EmailNotificationKind.StepActivated, submission.Id, currentUser.UserId.Value));
            
            return Result.Success();
        }

        response.ReviewerId = currentUser.UserId.Value;
        response.Result = request.Outcome;
        response.Remarks = request.Remarks ?? "";
        response.CompletedOn = DateTime.UtcNow;
        
        var route = await routingService.Advance(submission, currentStep, request.Outcome);
        
        if (!route.IsSuccess)
            return Result.Failure(route.Error!, route.ErrorDescription!);
        
        var result = route.Data!;
        
        if (result.outcome != RouteOutcome.Advanced && result.outcome != RouteOutcome.OnHold)
            submission.CompletedOn = DateTime.UtcNow;
        
        await context.SaveChangesAsync(cancellationToken);
        
        if (result.outcome is RouteOutcome.Advanced or RouteOutcome.Rejected)
            emailQueue.Enqueue(new EmailJob(EmailNotificationKind.StepActivated, submission.Id, currentUser.UserId.Value));

        if (result.outcome == RouteOutcome.Completed)
        {
            await TryPersistResultPdfAsync(submission, cancellationToken);
            emailQueue.Enqueue(new EmailJob(EmailNotificationKind.SubmissionCompleted, SubmissionId: submissionId));
        }

        await logService.LogAsync("Submission Responded", "Submission", submissionId, cancellationToken: cancellationToken);
        
        return Result.Success();
    }

    
    public async Task<Result> ResumeSubmissionAsync(Guid submissionId, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Result.Failure("UNAUTHORIZED", "User is not authenticated.");
        
        var submission = await context.Submissions
            .Include(s => s.Responses).ThenInclude(response => response.Assignee)
            .FirstOrDefaultAsync(s => s.Id == submissionId);
        
        if (submission is null)
            return Result.Failure("SUBMISSION_NOT_FOUND", "Submission not found.");
        
        if (submission.Status != Status.OnHold)
            return Result.Failure("INVALID_STATUS", "Submission is not on hold.");
        
        var responseId = Guid.NewGuid();
        
        var lastResponse = submission.Responses
            .FirstOrDefault(
                response => response.Result == Status.OnHold && response.StepId == submission.StepId);
        
        if (lastResponse is null)
            return Result.Failure("INVALID_STATUS", "Submission is not on hold.");
        
        context.Responses.Add(new Response {
            Id = responseId, 
            SubmissionId = submission.Id,
            StepId = submission.StepId,
            ActivatedOn = DateTime.UtcNow, 
            Assignee = lastResponse.Assignee,
            AssigneeId = lastResponse.AssigneeId
        });

        submission.Status = Status.Review;
        
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<SubmissionsPageDto>> GetSubmissionsReviewedAsync(Guid? processId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Result<SubmissionsPageDto>.Failure("UNAUTHORIZED", "User is not authenticated.");
        
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = context.Submissions
            .Include(s => s.Step)
            .Include(s => s.Submitter)
            .Include(s => s.Process)
            .Where(s => s.Responses.Any(r => 
                (r.ReviewerId == currentUser.UserId || r.AssigneeId == currentUser.UserId || currentUser.isSuperAdministrator) && r.CompletedOn != null));
        
        var categories = (await query
            .GroupBy(s => new { s.ProcessId, s.Process.Title })
            .Select(g => new SubmissionsCategoryDto(g.Key.ProcessId, g.Key.Title, g.Count()))
            .ToListAsync(cancellationToken))
            .OrderByDescending(c => c.Count)
            .ToList();
        
        if (processId.HasValue)
            query = query.Where(s => s.ProcessId == processId.Value);
        
        var totalCount = await query.CountAsync(cancellationToken);

        var submissions = await query
            .OrderByDescending(s => s.SubmittedOn)
            .ThenBy(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SubmissionSummaryDto(
                s.Id,
                s.Process.Title,
                s.Status,
                s.SubmittedOn,
                s.Step.Title,
                s.ProcessId
            ))
            .ToListAsync(cancellationToken);

        return Result<SubmissionsPageDto>.Success( new SubmissionsPageDto(submissions, categories, totalCount, page, pageSize));
    }

    public async Task<Result> RedirectSubmissionAsync(Guid submissionId, RedirectSubmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Result.Failure("UNAUTHORIZED", "User is not authenticated.");

        var submission = await context.Submissions
            .Include(s => s.Responses)
            .FirstOrDefaultAsync(s => s.Id == submissionId, cancellationToken);
        
        if (submission is null)
            return Result.Failure("SUBMISSION_NOT_FOUND", "Submission not found.");

        if (submission.Status != Status.Review)
            return Result.Failure("INVALID_STATUS", "Submission is not in pending review.");
        
        var response = submission.Responses
            .FirstOrDefault(r => r.StepId == submission.StepId && r.CompletedOn == null);

        if (response is null)
            return Result.Failure("RESPONSE_NOT_FOUND", "Response not found.");

        if (response.AssigneeId != currentUser.UserId && !currentUser.isSuperAdministrator)
            return Result.Failure("FORBIDDEN", "User is not authorized to perform this action.");
        
        var reviewer = await context.Users
            .FirstOrDefaultAsync(u => u.Email == request.NewReviewerEmail, cancellationToken);
        
        if (reviewer is null)
            return Result.Failure("INVALID_REVIEWER", "Reviewer not found.");
        
        if (reviewer.Id == submission.SubmitterId)
            return Result.Failure("FORBIDDEN", "Cannot assign reviewer to be the submitter.");
        
        if (reviewer.Id == response.AssigneeId)
            return Result.Failure("SAME_ASSIGNEE", "Cannot reassign to the same assignee.");
        
        var previousAssigneeId = response.AssigneeId;
        
        response.AssigneeId = reviewer.Id;
        response.Assignee = reviewer;

        await context.SaveChangesAsync(cancellationToken);
        
        await logService.LogAsync(
            "Submission Redirected", 
            "Submission", 
            submissionId, 
            metadata: $"from user {previousAssigneeId} to {reviewer.Id}", 
            cancellationToken: cancellationToken);

        return Result.Success();

    }

    public async Task<Result<PublicSubmissionStatusDto>> GetPublicSubmissionStatusAsync(string id, CancellationToken cancellationToken = default)
    {
        var normalized = id.ToUpperInvariant();
        if (normalized.StartsWith("KLD-"))
            normalized = normalized[4..];
        var isGuid = Guid.TryParse(id, out var guid);
        
        var submission = await context.Submissions
            .Include(s => s.Process).ThenInclude(p => p.Steps)
            .Include(s => s.Step)
            .Include(s => s.Responses).ThenInclude(r => r.Reviewer)
            .Include(s => s.Responses).ThenInclude(r => r.Assignee)
            .FirstOrDefaultAsync(s => 
                !s.IsDeleted && 
                (isGuid ? 
                    s.Id == guid : 
                    s.VerificationCode == normalized), 
                cancellationToken);

        if (submission is null)
            return Result<PublicSubmissionStatusDto>.Failure("SUBMISSION_NOT_FOUND", "Submission not found.");
        
        static string? Name(Data.User? u) => u is null ? null : $"{u.FirstName} {u.LastName}";

        var steps = submission.Process.Steps
            .Where(step => !step.IsDeleted)
            .OrderBy(step => step.Order)
            .Select(step =>
            {
                var latest = submission.Responses
                    .Where(r => !r.IsDeleted && r.StepId == step.Id)
                    .OrderByDescending(r => r.ActivatedOn)
                    .FirstOrDefault();

                string state;
                string? reviewer;
                DateTime? completedAt = null;

                if (latest is { CompletedOn: not null })
                {
                    state = latest.Result?.ToString() ?? "Done";
                    reviewer = Name(latest.Reviewer);
                    completedAt = latest.CompletedOn;
                }
                else if (latest is not null || step.Id == submission.StepId)
                {
                    state = submission.Status == Status.OnHold ? "OnHold" : "Current";
                    reviewer = Name(latest?.Assignee);
                }
                else
                {
                    state = "Pending";
                    reviewer = null;
                }

                // return new PublicStepStatusDto(step.Order, step.Title, state, reviewer, completedAt);
                return new PublicStepStatusDto(step.Order, step.Title, state, completedAt);
            }).ToList();
        
        return Result<PublicSubmissionStatusDto>
            .Success(new PublicSubmissionStatusDto(
                submission.Id,
                submission.Process.Title,
                submission.Status,
                submission.SubmittedOn,
                submission.CompletedOn,
                submission.Step.Title,
                steps,
                submission.ResultHash));
    }

    public async Task<Result<Guid>> CreateDocumentSubmissionAsync(
        CreateDocumentSubmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Result<Guid>.Failure("UNAUTHORIZED", "User is not authenticated.");

        var hasProcessId = Guid.TryParse(request.ProcessId, out var processId);
        
        if (!hasProcessId)
            return Result<Guid>.Failure("INVALID_PROCESS_ID", "Process ID is invalid.");
        
        var process = await context.Processes
            .Include(p => p.Steps.OrderBy(s => s.Order))
            .FirstOrDefaultAsync(p => p.Id == processId, cancellationToken);
        
        if (process is null)
            return Result<Guid>.Failure("PROCESS_NOT_FOUND", "Process not found.");
        
        if (process.Kind != ProcessKind.FreeDocument)
            return Result<Guid>.Failure("INVALID_PROCESS_KIND", "Process kind must be FreeDocument.");
        
        if (!process.IsActive)
            return Result<Guid>.Failure("PROCESS_INACTIVE", "Process is inactive.");
        
        var liveSteps = process.Steps.Where(s => !s.IsDeleted).ToList();
        
        if (liveSteps.Count == 0)
            return Result<Guid>.Failure("INVALID_PROCESS", "Process has no steps.");
        
        if (request.Slots is null)
            return Result<Guid>.Failure("INVALID_SLOTS", "Slots are required.");
        
        if (request.Route is null)
            return Result<Guid>.Failure("INVALID_ROUTE", "Route is required.");
        
        if (liveSteps.Count != request.Slots.Count)
            return Result<Guid>.Failure("INVALID_SLOTS_COUNT", "Slots count does not match process steps count.");
        
        if (request.Route.Count != request.Slots.Count)
            return Result<Guid>.Failure("INVALID_ROUTE_COUNT", "Route count does not match slots count.");
        
        var submissionId = Guid.NewGuid();
        
        if (request.File is null || request.File.Length == 0)
            return Result<Guid>.Failure("INVALID_FILE", "A PDF file is required.");
        if (request.File.Length > 10_000_000)
            return Result<Guid>.Failure("INVALID_FILE_SIZE", "File exceeds the 10 MB limit.");
        
        await using var stream = request.File.OpenReadStream();

        byte[] header = new byte[5];
        if (await stream.ReadAsync(header.AsMemory(0, 5), cancellationToken) != 5 ||
            header[0] != '%' || header[1] != 'P' || header[2] != 'D' || header[3] != 'F' || header[4] != '-')
            return Result<Guid>.Failure("INVALID_FILE_TYPE", "File is not a PDF.");

        stream.Position = 0;

        int pageCount;
        try
        {
            using var pdf = new PdfDocument(new PdfReader(stream));
            pageCount = pdf.GetNumberOfPages();
        }
        catch (Exception)
        {
            return Result<Guid>.Failure("INVALID_FILE_TYPE", "File is not a readable PDF (corrupt or password-protected).");
        }

        var validated = await ValidateRouteAsync(request.Route, request.Slots, submissionId, pageCount, cancellationToken);
        
        if (!validated.IsSuccess)
            return Result<Guid>.Failure(validated.Error, validated.ErrorDescription);

        stream.Position = 0;

        
        var verificationCode = await GenerateVerificationCodeAsync(process, cancellationToken);
        
        var firstStep = process.Steps.OrderBy(s => s.Order).First(s => !s.IsDeleted);
        
        var requestSteps = request.Route.Select(r => r.StepId).ToHashSet();
        var processSteps = process.Steps.Where(s => !s.IsDeleted).Select(s => s.Id).ToHashSet();

        if (!requestSteps.SetEquals(processSteps))
            return Result<Guid>.Failure("INVALID_ROUTE", "Route does not match process steps");
        
        var path = $"{submissionId:N}/source/{Path.GetFileName(request.File.FileName)}";
        var sourceLink = await storageService.UploadAsync(stream, path, "application/pdf", Prefix.Document, cancellationToken);

        var submission = new Data.Submission
        {
            Id = submissionId,
            SubmitterId = currentUser.UserId.Value,
            ProcessId = processId,
            SubmittedOn = DateTime.UtcNow,
            SourceLink = sourceLink,
            SourceTitle = Path.GetFileName(request.File.FileName),
            VerificationCode = verificationCode
        };
        
        var start = await routingService.StartRoute(submission, process,  validated.Data.FirstOrDefault(r => r.StepId == firstStep.Id));
        
        if (!start.IsSuccess)
            return Result<Guid>.Failure(start.Error!, start.ErrorDescription!);

        context.Submissions.Add(submission);
        
        await context.SaveChangesAsync(cancellationToken);
        
        await logService.LogAsync("Submission Created", "Submission", submissionId);
        
        emailQueue.Enqueue(new EmailJob(EmailNotificationKind.SubmissionReceived, SubmissionId: submissionId));

        return Result<Guid>.Success(submission.Id);
    }

    public async Task<Result<DocumentDownloadDto>> DownloadDocumentAsync(Guid submissionId, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Result<DocumentDownloadDto>.Failure("UNAUTHORIZED", "User is not authenticated.");

        var submission = await context.Submissions
            .Include(s => s.Responses)
            .FirstOrDefaultAsync(submission => 
                                 submission.Id == submissionId, cancellationToken);
        
        if (submission is null)
            return Result<DocumentDownloadDto>.Failure("SUBMISSION_NOT_FOUND", "Submission not found.");
        
        if (currentUser.UserId != submission.SubmitterId && 
            !currentUser.isSuperAdministrator &&
            !submission.Responses.Any(r => r.ReviewerId == currentUser.UserId || r.AssigneeId == currentUser.UserId))
            return Result<DocumentDownloadDto>.Failure("UNAUTHORIZED", "You are not authorized to access this submission.");
        
        if (string.IsNullOrEmpty(submission.SourceLink))
            return Result<DocumentDownloadDto>.Failure("NO_SOURCE_DOCUMENT", "This submission has no source document.");

        var stream = await storageService.DownloadAsync(submission.SourceLink, cancellationToken);
        
        return Result<DocumentDownloadDto>.Success(new DocumentDownloadDto(stream, submission.SourceTitle,  "application/pdf"));
    }

    public async Task<Result> CancelSubmissionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Result.Failure("UNAUTHORIZED", "User is not authenticated.");

        var submission = await context.Submissions
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (submission is null)
            return Result.Failure("SUBMISSION_NOT_FOUND", "Submission not found.");

        if (submission.SubmitterId != currentUser.UserId.Value && !currentUser.isSuperAdministrator)
            return Result.Failure("FORBIDDEN", "You can only cancel your own submissions.");

        if (submission.Status == Status.Approve || submission.Status == Status.Reject || submission.Status == Status.Cancel)
            return Result.Failure("INVALID_STATUS", "Cannot cancel a submission that is already completed.");

        submission.Status = Status.Cancel;
        submission.CompletedOn = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        
        if (submission.SubmitterId != currentUser.UserId.Value && currentUser.isSuperAdministrator) 
            await logService.LogAsync("Submission Cancelled", "Submission", id, metadata: "Cancelled by admin, not submitter", cancellationToken: cancellationToken);
        else 
            await logService.LogAsync("Submission Cancelled", "Submission", id, metadata: "Cancelled by submitter", cancellationToken: cancellationToken);
        
        
        return Result.Success();
    }
    
    public async Task<Result<List<Attachment>>> UploadAttachmentsAsync(
        Guid submissionId,
        IReadOnlyList<AttachmentUploadDto> attachments,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Result<List<Attachment>>.Failure("UNAUTHORIZED", "User is not authenticated.");

        var submission = await context.Submissions
            .FirstOrDefaultAsync(submission =>
                submission.Id == submissionId, cancellationToken);
        
        if (submission is null)
            return Result<List<Attachment>>.Failure("SUBMISSION_NOT_FOUND", "Submission not found.");

        if (submission.SubmitterId != currentUser.UserId.Value)
            return Result<List<Attachment>>.Failure("FORBIDDEN", "You can only upload attachments to your own submissions.");
        
        var results = new List<Attachment>();

        foreach (var file in attachments)
        {
            var attachmentId = Guid.NewGuid();
            var key = await storageService.UploadAsync(
                file.Content, 
                $"{submissionId}/{attachmentId}/{Path.GetFileName(file.FileName)}", 
                file.ContentType, 
                Prefix.Attachment, 
                cancellationToken);
            
            results.Add(new Attachment
            {
                Id = attachmentId,
                SubmissionId = submissionId,
                Title = file.FileName,
                Type = file.ContentType,
                Link = key
            });
        }
        
        context.Attachments.AddRange(results);
        await context.SaveChangesAsync(cancellationToken);
        
        return Result<List<Attachment>>.Success(results);
    }

    public async Task<Result> DeleteAttachmentAsync(
        Guid submissionId, 
        Guid attachmentId, 
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Result.Failure("UNAUTHORIZED", "User is not authenticated.");

        var submission = await context.Submissions
                             .FirstOrDefaultAsync(submission => 
                                 submission.Id == submissionId, cancellationToken);
        
        if (submission is null)
            return Result.Failure("SUBMISSION_NOT_FOUND", "Submission not found.");
        
        if (submission.SubmitterId != currentUser.UserId)
            return Result.Failure("UNAUTHORIZED", "User is not authorized to delete this attachment.");

        var attachment = await context.Attachments
            .FirstOrDefaultAsync(attachment =>
                    attachment.SubmissionId == submissionId &&
                    attachment.Id == attachmentId,
                cancellationToken);
            
        if (attachment is null)
            return Result.Failure("ATTACHMENT_NOT_FOUND", "Attachment not found.");

        context.Attachments.Remove(attachment);
        
        await context.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<AttachmentDto>>> GetAttachmentsAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Result<IReadOnlyList<AttachmentDto>>.Failure("UNAUTHORIZED", "User is not authenticated.");

        var submission = await context.Submissions
            .Include(submission => submission.Responses)
            .ThenInclude(response => response.Reviewer)
            .FirstOrDefaultAsync(submission => 
                                 submission.Id == submissionId, cancellationToken);
        
        if (submission is null)
            return Result<IReadOnlyList<AttachmentDto>>.Failure("SUBMISSION_NOT_FOUND", "Submission not found.");
       
        if (currentUser.UserId != submission.SubmitterId && 
            !currentUser.isSuperAdministrator &&
            !submission.Responses.Any(r => r.Reviewer?.Id == currentUser.UserId || r.AssigneeId == currentUser.UserId))
            return Result<IReadOnlyList<AttachmentDto>>.Failure("UNAUTHORIZED", "You are not authorized to access this submission.");
        
        var attachments = await context.Attachments
            .Where(attachment => 
                attachment.SubmissionId == submissionId && !attachment.IsDeleted)
            .Select(attachment => new AttachmentDto(
                attachment.Id,
                attachment.Title,
                attachment.Type))
            .ToListAsync(cancellationToken);
        
        return Result<IReadOnlyList<AttachmentDto>>.Success(attachments);
    }

    public async Task<Result<AttachmentDownloadDto>> DownloadAttachmentAsync(
        Guid submissionId,
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Result<AttachmentDownloadDto>.Failure("UNAUTHORIZED", "User is not authenticated.");

        var submission = await context.Submissions
            .Include(s => s.Responses)
            .FirstOrDefaultAsync(submission => 
                                 submission.Id == submissionId, cancellationToken);
        
        if (submission is null)
            return Result<AttachmentDownloadDto>.Failure("SUBMISSION_NOT_FOUND", "Submission not found.");
        
        if (currentUser.UserId != submission.SubmitterId && 
            !currentUser.isSuperAdministrator &&
            !submission.Responses.Any(r => r.ReviewerId == currentUser.UserId || r.AssigneeId == currentUser.UserId))
            return Result<AttachmentDownloadDto>.Failure("UNAUTHORIZED", "You are not authorized to access this submission.");

        var attachment = await context.Attachments
            .FirstOrDefaultAsync(attachment =>
                    attachment.SubmissionId == submissionId &&
                    attachment.Id == attachmentId && 
                    !attachment.IsDeleted,
                cancellationToken);
            
        if (attachment is null)
            return Result<AttachmentDownloadDto>.Failure("ATTACHMENT_NOT_FOUND", "Attachment not found.");

        var stream = await storageService.DownloadAsync(attachment.Link, cancellationToken);
        
        return Result<AttachmentDownloadDto>.Success(new AttachmentDownloadDto(stream, attachment.Title, attachment.Type));
    }
    
    private async Task TryPersistResultPdfAsync(Data.Submission submission, CancellationToken cancellationToken)
    {
        try
        {
            await using var pdf = await pdfGenerationService.GenerateAsync(submission.Id, cancellationToken);

            using var buffer = new MemoryStream();
            await pdf.CopyToAsync(buffer, cancellationToken);
            var bytes = buffer.ToArray();
            
            var key = await storageService.UploadAsync(
                new MemoryStream(bytes), 
                $"{submission.Id:N}/result.pdf", 
                "application/pdf", 
                Prefix.Output, 
                cancellationToken);
            
            submission.ResultLink = key;
            submission.ResultLinkGeneratedOn = DateTime.UtcNow;
            submission.ResultHash = Convert.ToHexString(SHA256.HashData(bytes));
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            await logService.LogAsync(
                "Result PDF Persist Failed", 
                "Submission", 
                submission.Id, 
                e.ToString(), 
                cancellationToken);
        }
    }
}