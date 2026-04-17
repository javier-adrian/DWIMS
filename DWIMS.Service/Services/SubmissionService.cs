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
    public Task<Result<IReadOnlyList<SubmissionSummaryDto>>> GetMySubmissionsAsync(Status? statusFilter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<IReadOnlyList<PendingReviewDto>>> GetPendingReviewAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
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

    public Task<Result> RespondToStepAsync(Guid submissionId, Guid stepId, RespondToStepRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result> DeleteSubmissionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}