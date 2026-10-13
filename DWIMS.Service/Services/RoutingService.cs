using DWIMS.Data;
using DWIMS.Service.Routing;

namespace DWIMS.Service.Services;

public class RoutingService(AppDbContext context) : IRoutingService
{
    public void StartRoute(Data.Submission submission, Data.Process process)
    {
        var firstStep = process.Steps.FirstOrDefault() 
                    ?? throw new InvalidOperationException("Process has no steps");
        
        submission.StepId = firstStep.Id;
        submission.Status = Status.Review;
        
        context.Responses.Add(new Response {
            Id = Guid.NewGuid(), 
            SubmissionId = submission.Id,
            StepId = firstStep.Id,
            ActivatedOn = DateTime.UtcNow, 
        });
    }

    public RouteResult Advance(Data.Submission submission, Step currentStep, Status outcome)
    {
        if (outcome == Status.Reject)
        {
            submission.Status = Status.Reject;
            
            return new RouteResult (
                RouteOutcome.Rejected,
                null
            );
        }
        
        var nextStep = currentStep.Process.Steps.FirstOrDefault(step => step.Order > currentStep.Order);

        if (nextStep is not null)
        {
            submission.StepId = nextStep.Id;
            submission.Status = Status.Review;
            
            context.Responses.Add(new Response {
                Id = Guid.NewGuid(), 
                SubmissionId = submission.Id,
                StepId = nextStep.Id,
                ActivatedOn = DateTime.UtcNow, 
            });
            
            return new RouteResult(RouteOutcome.Advanced, nextStep);
        }

        submission.Status = Status.Approve;
        
        return new RouteResult(RouteOutcome.Completed, null);
    }
}