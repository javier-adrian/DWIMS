using DWIMS.Data;

namespace DWIMS.Service.Routing;

public interface IRoutingService
{
    void StartRoute(Data.Submission submission, Data.Process process);
    
    RouteResult Advance(Data.Submission submission, Step currentStep, Status outcome);
}