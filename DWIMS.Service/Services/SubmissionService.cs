using System.Text.Json;
using DWIMS.Data;
using DWIMS.Service.Common;
using DWIMS.Service.Logs;
using DWIMS.Service.Routing;
using DWIMS.Service.Storage;
using DWIMS.Service.Submission;
using DWIMS.Service.Submission.Dtos;
using DWIMS.Service.Submission.Requests;
using DWIMS.Service.User;
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
    
    private record CheckBoxOptions(string On);

    private record FieldOptions(string Value, string Label);
    
    public async Task<Result<IReadOnlyList<SubmissionSummaryDto>>> GetMySubmissionsAsync(Status? statusFilter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Result<IReadOnlyList<SubmissionSummaryDto>>.Failure("UNAUTHORIZED", "User is not authenticated.");

        var query = context.Submissions
            .Include(s => s.Step)
            .Where(s => s.SubmitterId == currentUser.UserId.Value);

        if (statusFilter.HasValue)
            query = query.Where(s => s.Status == statusFilter.Value);

        var submissions = await query
            .OrderByDescending(s => s.SubmittedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SubmissionSummaryDto(
                s.Id,
                s.Process.Title,
                s.Status,
                s.SubmittedOn,
                s.Step.Title
            ))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<SubmissionSummaryDto>>.Success(submissions);
    }

    public async Task<Result<IReadOnlyList<PendingReviewDto>>> GetPendingReviewAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Result<IReadOnlyList<PendingReviewDto>>.Failure("UNAUTHORIZED", "User is not authenticated.");

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
        

        var pending = await query
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

        return Result<IReadOnlyList<PendingReviewDto>>.Success(pending);
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


        return Result<SubmissionDetailDto>.Success(new SubmissionDetailDto(
            submission.Id,
            submission.ProcessId,
            submission.Process.Title,
            submission.Status,
            submission.SubmittedOn,
            submission.CompletedOn,
            submission.Step.Title,
            stepResponses,
            fieldValues,
            attachmentDtos
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

        return Result<SubmissionDetailDto>.Success(new SubmissionDetailDto(
            submission.Id,
            submission.ProcessId,
            submission.Process.Title,
            submission.Status,
            submission.SubmittedOn,
            submission.CompletedOn,
            submission.Step.Title,
            stepResponses,
            fieldValues,
            attachmentDtos
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
        
        var submission = new Data.Submission
        {
            Id = Guid.NewGuid(),
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
        
        if (request.Outcome is Status.OnHold && string.IsNullOrWhiteSpace(request.Remarks))
            return Result.Failure("REASON_REQUIRED", "Remarks must be provided when outcome is OnHold.");

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

    public async Task<Result<IReadOnlyList<SubmissionSummaryDto>>> GetSubmissionsReviewedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Result<IReadOnlyList<SubmissionSummaryDto>>.Failure("UNAUTHORIZED", "User is not authenticated.");

        var query = context.Submissions
            .Include(s => s.Step)
            .Include(s => s.Submitter)
            .Include(s => s.Process)
            .Where(s => s.Responses.Any(r => 
                (r.ReviewerId == currentUser.UserId || r.AssigneeId == currentUser.UserId || currentUser.isSuperAdministrator) && r.CompletedOn != null));

        var submissions = await query
            .OrderByDescending(s => s.SubmittedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SubmissionSummaryDto(
                s.Id,
                s.Process.Title,
                s.Status,
                s.SubmittedOn,
                s.Step.Title
            ))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<SubmissionSummaryDto>>.Success(submissions);
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

    public async Task<Result<PublicSubmissionStatusDto>> GetPublicSubmissionStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var submission = await context.Submissions
            .Include(s => s.Process).ThenInclude(p => p.Steps)
            .Include(s => s.Step)
            .Include(s => s.Responses).ThenInclude(r => r.Reviewer)
            .Include(s => s.Responses).ThenInclude(r => r.Assignee)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

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
                steps));
    }

    public async Task<Result> CancelSubmissionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Result.Failure("UNAUTHORIZED", "User is not authenticated.");

        var submission = await context.Submissions
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (submission is null)
            return Result.Failure("SUBMISSION_NOT_FOUND", "Submission not found.");

        if (submission.SubmitterId != currentUser.UserId.Value)
            return Result.Failure("FORBIDDEN", "You can only cancel your own submissions.");

        if (submission.Status == Status.Approve || submission.Status == Status.Reject || submission.Status == Status.Cancel)
            return Result.Failure("INVALID_STATUS", "Cannot cancel a submission that is already completed.");

        submission.Status = Status.Cancel;
        submission.CompletedOn = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        
        await logService.LogAsync("Submission Cancelled", "Submission", id, cancellationToken: cancellationToken);
        
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
            
            var key = await storageService.UploadAsync(
                pdf, 
                $"{submission.Id:N}/result.pdf", 
                "application/pdf", 
                Prefix.Output, 
                cancellationToken);
            
            submission.ResultLink = key;
            submission.ResultLinkGeneratedOn = DateTime.UtcNow;
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