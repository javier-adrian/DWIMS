using DWIMS.Service.Services;
using DWIMS.Service.Submission;

namespace DWIMS.Controllers;

public static class PublicEndpoints
{
    public static IEndpointRouteBuilder MapPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("public")
            .WithTags("Public")
            .AllowAnonymous();
        
        group.MapGet("/submission/{id:guid}", GetPublicSubmissionStatus)
            .WithDisplayName("Get Public Submission Status")
            .WithSummary("Get the public status of a submission");

        return app;
    }
    
    private static async Task<IResult> GetPublicSubmissionStatus(
        Guid id,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        var result = await submissionService.GetPublicSubmissionStatusAsync(id, cancellationToken);
        return result.ToOkResult();
    }
}