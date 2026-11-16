using DWIMS.Data;
using DWIMS.Service.Common;
using DWIMS.Service.Routing;
using Microsoft.EntityFrameworkCore;

namespace DWIMS.Service.Services;

public class RoutingService(AppDbContext context) : IRoutingService
{
    public async Task<Result<Guid>> StartRoute(
        Data.Submission submission, 
        Data.Process process, 
        SubmissionRoute route,
        IReadOnlyDictionary<Guid, string>? fieldValues = null)
    {
        var values = fieldValues ?? await context.Inputs
            .Where(input => input.SubmissionId == submission.Id && !input.IsDeleted)
            .ToDictionaryAsync(input => input.FieldId, input => input.Value);
        
        var firstStep = process.Steps
                            .Where(step => !step.IsDeleted)
                            .OrderBy(step => step.Order)
                            .FirstOrDefault(step => RoutingRules.ShouldInclude(step, values)) 
                    ?? throw new InvalidOperationException("Process has no steps for this submission.");
        
        var reviewer = await context.Users
            .FirstOrDefaultAsync(reviewer => reviewer.Id == route.AssigneeId);

        if (reviewer is null)
            return Result<Guid>.Failure("INVALID_REVIEWER", "Assigned reviewer no longer exists.");
        
        if (submission.SubmitterId == reviewer.Id)
            return Result<Guid>.Failure("FORBIDDEN", "Cannot Assign to yourself.");

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

    public async Task<Result<RouteResult>> Advance(Data.Submission submission, Step currentStep, Status outcome)
    {
        if (outcome == Status.Reject)
        {
            submission.Status = Status.Reject;
            
            return Result<RouteResult>.Success(new RouteResult (
                RouteOutcome.Rejected,
                null
            ));
        }
        
        var values = await context.Inputs
            .Where(input => input.SubmissionId == submission.Id && !input.IsDeleted)
            .ToDictionaryAsync(input => input.FieldId, input => input.Value);

        var candidates = currentStep.Process.Steps
            .Where(step => step.Order > currentStep.Order && !step.IsDeleted)
            .OrderBy(step => step.Order)
            .ToList();

        var skipped = candidates
            .Where(step => !RoutingRules.ShouldInclude(step, values))
            .ToList();
        
        var nextStep = candidates.FirstOrDefault(step => !skipped.Contains(step));

        if (nextStep is null)
        {
            submission.Status = Status.Approve;
            return Result<RouteResult>
                .Success(
                    new RouteResult(
                        RouteOutcome.Completed, 
                        null,
                        await DescribeSkipsAsync(skipped, values)));
        }
        
        var nextRoute = await context.SubmissionRoutes.FirstOrDefaultAsync(route => route.SubmissionId == submission.Id && route.StepId == nextStep.Id);
        
        if (nextRoute is null)
            return Result<RouteResult>.Failure("ROUTE_INCOMPLETE", "Route for next step is missing.");
        
        var reviewer = await context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(reviewer => reviewer.Id == nextRoute.AssigneeId);

        if (reviewer is null)
            return Result<RouteResult>.Failure("INVALID_REVIEWER", "Assigned reviewer no longer exists.");
        
        if (reviewer.IsDeleted)
            return Result<RouteResult>.Failure("DEACTIVATED_REVIEWER", "Reviewer is a deactivated user.");
        
        if (submission.SubmitterId == reviewer.Id)
            return Result<RouteResult>.Failure("FORBIDDEN", "Cannot Assign to submitter.");

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
        
        return Result<RouteResult>.Success(new RouteResult(RouteOutcome.Advanced, nextStep, await DescribeSkipsAsync(skipped, values)));
    }

    private async Task<List<string>> DescribeSkipsAsync(
        List<Step> skipped,
        IReadOnlyDictionary<Guid, string> values)
    {
        if (skipped.Count == 0) return [];

        var titles = await context.Fields
            .Where(f => skipped.Select(s => s.ConditionFieldId!.Value).Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, f => f.Title);

        return skipped
            .Select(step => RoutingRules.Describe(step,
                titles.GetValueOrDefault(step.ConditionFieldId!.Value),
                values.GetValueOrDefault(step.ConditionFieldId!.Value)))
            .ToList();
    }
}