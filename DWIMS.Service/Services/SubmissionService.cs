using DWIMS.Data;
using DWIMS.Service.Common;
using DWIMS.Service.Submission;
using DWIMS.Service.Submission.Dtos;
using DWIMS.Service.Submission.Requests;
using DWIMS.Service.User;
using Microsoft.EntityFrameworkCore;

namespace DWIMS.Service.Services;

public class SubmissionService(AppDbContext context, ICurrentUserService currentUser) : ISubmissionService
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

        query = query.Where(s => !s.Responses.Any(r => r.ReviewerId == currentUser.UserId.Value));

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

    public Task<Result<SubmissionDetailDto>> GetPendingReviewAsync(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
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
            Status = Status.Submit,
            SubmittedOn = DateTime.UtcNow,
            CompletedOn = DateTime.UtcNow,
            SubmitterId = currentUser.UserId.Value,
        };

        context.Submissions.Add(submission);

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
            .FirstOrDefaultAsync(s => s.Id == submissionId, cancellationToken);

        if (submission is null)
            return Result.Failure("SUBMISSION_NOT_FOUND", "Submission not found.");

        if (submission.StepId != stepId)
            return Result.Failure("WRONG_STEP", "This submission is not on the specified step.");

        var currentStep = submission.Process.Steps.FirstOrDefault(s => s.Id == stepId);
        if (currentStep is null)
            return Result.Failure("STEP_NOT_FOUND", "Step not found.");

        context.Responses.Add(new Response
        {
            Id = Guid.NewGuid(),
            SubmissionId = submissionId,
            StepId = stepId,
            ReviewerId = currentUser.UserId.Value,
            Result = request.Outcome,
            Remarks = request.Remarks ?? "",
            SubmittedOn = DateTime.UtcNow,
            CompletedOn = DateTime.UtcNow,
        });

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
            }
            else
            {
                submission.Status = Status.Approve;
            }
        }

        submission.CompletedOn = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public Task<Result> DeleteSubmissionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}