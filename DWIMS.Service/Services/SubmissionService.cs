using DWIMS.Data;
using DWIMS.Service.Common;
using DWIMS.Service.Logs;
using DWIMS.Service.Submission;
using DWIMS.Service.Submission.Dtos;
using DWIMS.Service.Submission.Requests;
using DWIMS.Service.User;
using Microsoft.EntityFrameworkCore;

namespace DWIMS.Service.Services;

public class SubmissionService(
    AppDbContext context, 
    ICurrentUserService currentUser,
    INotificationService notificationService,
    ILogService logService) 
    : ISubmissionService
{
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

        var departmentIds = currentUser.Roles
            .Where(r => r.Value >= GeneralRole.Reviewer && r.Key != Guid.Empty)
            .Select(r => r.Key)
            .ToList();

        if (departmentIds.Count == 0 && !currentUser.isSuperAdministrator)
            return Result<IReadOnlyList<PendingReviewDto>>.Success([]);

        var query = context.Submissions
            .Include(s => s.Step)
            .Include(s => s.Submitter)
            .Include(s => s.Process)
            .Include(s => s.Responses)
            .Where(s => s.Status == Status.Review);

        if (!currentUser.isSuperAdministrator)
            query = query.Where(s => departmentIds.Contains(s.Step.DepartmentId));

        query = query.Where(s => s.SubmitterId != currentUser.UserId.Value);
        query = query.Where(s => !s.Responses.Any(r => r.StepId == s.StepId && r.ReviewerId == currentUser.UserId.Value && r.CompletedOn != null));

        var pending = await query
            .Select(s => new PendingReviewDto(
                s.Id,
                s.Process.Id,
                s.Process.Title,
                s.Step.Title,
                $"{s.Submitter.FirstName} {s.Submitter.LastName}",
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
            .Include(s => s.Responses).ThenInclude(r => r.Reviewer).Include(submission => submission.Responses)
            .ThenInclude(response => response.Step)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (submission is null)
            return Result<SubmissionDetailDto>.Failure("SUBMISSION_NOT_FOUND", "Submission not found.");

        var stepResponses = submission.Responses.Select(r => new SubmissionStepResponseDto(
            r.Id,
            r.Step.Title,
            r.Result?.ToString(),
            r.Remarks,
            r.Reviewer is not null ? $"{r.Reviewer.FirstName} {r.Reviewer.LastName}" : null,
            r.ActivatedOn,
            r.CompletedOn
        )).ToList();

        var fieldValues = submission.Inputs.Select(i => new SubmissionFieldValueDto(
            i.Field.Title,
            i.Field.Type.ToString(),
            i.Value
        )).ToList();

        return Result<SubmissionDetailDto>.Success(new SubmissionDetailDto(
            submission.Id,
            submission.ProcessId,
            submission.Process.Title,
            submission.Status,
            submission.SubmittedOn,
            submission.CompletedOn,
            submission.Step.Title,
            stepResponses,
            fieldValues
        ));
    }

    public async Task<Result<SubmissionDetailDto>> GetSubmissionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var submission = await context.Submissions
            .Include(s => s.Process)
            .Include(s => s.Step)
            .Include(s => s.Submitter)
            .Include(s => s.Inputs).ThenInclude(i => i.Field)
            .Include(s => s.Responses).ThenInclude(r => r.Reviewer)
            .Include(s => s.Responses).ThenInclude(r => r.Step)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (submission is null)
            return Result<SubmissionDetailDto>.Failure("SUBMISSION_NOT_FOUND", "Submission not found.");

        var stepResponses = submission.Responses.Select(r => new SubmissionStepResponseDto(
            r.Id,
            r.Step.Title,
            r.Result?.ToString(),
            r.Remarks,
            r.Reviewer is not null ? $"{r.Reviewer.FirstName} {r.Reviewer.LastName}" : null,
            r.ActivatedOn,
            r.CompletedOn
        )).ToList();

        var fieldValues = submission.Inputs.Select(i => new SubmissionFieldValueDto(
            i.Field.Title,
            i.Field.Type.ToString(),
            i.Value
        )).ToList();

        return Result<SubmissionDetailDto>.Success(new SubmissionDetailDto(
            submission.Id,
            submission.ProcessId,
            submission.Process.Title,
            submission.Status,
            submission.SubmittedOn,
            submission.CompletedOn,
            submission.Step.Title,
            stepResponses,
            fieldValues
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

        var firstStep = process.Steps.FirstOrDefault()
            ?? throw new InvalidOperationException("Process has no steps.");

        var submission = new Data.Submission
        {
            Id = Guid.NewGuid(),
            ProcessId = process.Id,
            StepId = firstStep.Id,
            Status = Status.Review,
            SubmittedOn = DateTime.UtcNow,
            CompletedOn = DateTime.UtcNow,
            SubmitterId = currentUser.UserId.Value,
        };

        context.Submissions.Add(submission);

        context.Responses.Add(new Response
        {
            Id = Guid.NewGuid(),
            SubmissionId = submission.Id,
            StepId = firstStep.Id,
            ActivatedOn = DateTime.UtcNow,
        });

        foreach (var fieldInput in request.Fields)
        {
            var fieldExists = process.Fields.Any(f => f.Id == fieldInput.FieldId);
            if (!fieldExists)
                return Result<Guid>.Failure("INVALID_FIELD", $"Field {fieldInput.FieldId} does not belong to this process.");

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
        
        await notificationService.SendSubmissionReceivedAsync(submission.Id, cancellationToken);
        
        return Result<Guid>.Success(submission.Id);
    }

    public async Task<Result> RespondToStepAsync(Guid submissionId, Guid stepId, RespondToStepRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is null)
            return Result.Failure("UNAUTHORIZED", "User is not authenticated.");

        if (request.Outcome is not Status.Approve and not Status.Reject)
            return Result.Failure("INVALID_OUTCOME", "Outcome must be Approve or Reject.");

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

        response.ReviewerId = currentUser.UserId.Value;
        response.Result = request.Outcome;
        response.Remarks = request.Remarks ?? "";
        response.CompletedOn = DateTime.UtcNow;

        if (request.Outcome == Status.Reject)
        {
            submission.Status = Status.Reject;
        }
        else
        {
            var nextStep = submission.Process.Steps
                .FirstOrDefault(s => s.Order > currentStep.Order);

            if (nextStep is not null)
            {
                submission.StepId = nextStep.Id;
                submission.Status = Status.Review;

                context.Responses.Add(new Response
                {
                    Id = Guid.NewGuid(),
                    SubmissionId = submissionId,
                    StepId = nextStep.Id,
                    ActivatedOn = DateTime.UtcNow,
                });
            }
            else
            {
                submission.Status = Status.Approve;
            }
        }

        if (submission.Status != Status.Review)
            submission.CompletedOn = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);

        await logService.LogAsync("Submission Responded", "Submission", submissionId, cancellationToken: cancellationToken);
        
        return Result.Success();
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
}