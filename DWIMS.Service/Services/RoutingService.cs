using DWIMS.Data;
using DWIMS.Service.Common;
using DWIMS.Service.Routing;
using Microsoft.EntityFrameworkCore;

namespace DWIMS.Service.Services;

public class RoutingService(AppDbContext context) : IRoutingService
{
    public async Task<Result<Guid>> StartRoute(Data.Submission submission, Data.Process process, string firstReviewerEmail)
    {
        var firstStep = process.Steps.FirstOrDefault(step => !step.IsDeleted) 
                    ?? throw new InvalidOperationException("Process has no steps");
        
        var reviewer = await context.Users
            .FirstOrDefaultAsync(reviewer => reviewer.Email == firstReviewerEmail);

        if (reviewer is null)
        {
            return Result<Guid>.Failure("INVALID_REVIEWER", "No user with such email.");
        }
        
        if (submission.SubmitterId == reviewer.Id)
        {
            return Result<Guid>.Failure("FORBIDDEN", "Cannot Assign to yourself.");
        }

        submission.StepId = firstStep.Id;
        submission.Status = Status.Review;
        
        var responseId = Guid.NewGuid();
        
        context.Responses.Add(new Response {
            Id = responseId, 
            SubmissionId = submission.Id,
            StepId = firstStep.Id,
            ActivatedOn = DateTime.UtcNow, 
            Assignee = reviewer,
            AssigneeId = reviewer.Id
        });
        
        return Result<Guid>.Success(responseId);
    }

    public async Task<Result<RouteResult>> Advance(Data.Submission submission, Step currentStep, Status outcome, string nextReviewerEmail)
    {
        if (outcome == Status.Reject)
        {
            submission.Status = Status.Reject;
            
            return Result<RouteResult>.Success(new RouteResult (
                RouteOutcome.Rejected,
                null
            ));
        }
        
        var nextStep = currentStep.Process.Steps.FirstOrDefault(step => step.Order > currentStep.Order && !step.IsDeleted);

        if (nextStep is null)
        {
            submission.Status = Status.Approve;
            return Result<RouteResult>.Success(new RouteResult(RouteOutcome.Completed, null));
        }
        
        var reviewer = await context.Users
            .FirstOrDefaultAsync(reviewer => reviewer.Email == nextReviewerEmail);

        if (reviewer is null)
        {
            return Result<RouteResult>.Failure("INVALID_REVIEWER", "No user with such email.");
        }
        
        if (submission.SubmitterId == reviewer.Id)
        {
            return Result<RouteResult>.Failure("FORBIDDEN", "Cannot Assign to submitter.");
        }

        submission.StepId = nextStep.Id;
        submission.Status = Status.Review;
        
        context.Responses.Add(new Response {
            Id = Guid.NewGuid(), 
            SubmissionId = submission.Id,
            StepId = nextStep.Id,
            ActivatedOn = DateTime.UtcNow, 
            AssigneeId = reviewer.Id,
            Assignee = reviewer
        });
        
        return Result<RouteResult>.Success(new RouteResult(RouteOutcome.Advanced, nextStep));
    }
}