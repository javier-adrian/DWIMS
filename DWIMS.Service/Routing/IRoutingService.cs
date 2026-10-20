using DWIMS.Data;
using DWIMS.Service.Common;

namespace DWIMS.Service.Routing;

public interface IRoutingService
{
    Task<Result<Guid>> StartRoute(Data.Submission submission, Data.Process process, SubmissionRoute route);
    
    Task<Result<RouteResult>> Advance(Data.Submission submission, Step currentStep, Status outcome);
}