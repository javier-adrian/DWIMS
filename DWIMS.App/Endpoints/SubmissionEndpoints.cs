using DWIMS.Service.Auth;
using DWIMS.Service.Services;
using DWIMS.Service.Submission;
using DWIMS.Service.Submission.Requests;

namespace DWIMS.Controllers;

public static class SubmissionEndpoints
{
    public static IEndpointRouteBuilder MapSubmissionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("submission")
            .WithTags("Submission")
            .RequireAuthorization();

        group.MapPost("/", CreateSubmission);
        group.MapGet("/own", GetMySubmissions);
        group.MapGet("/review", GetSubmissionsToReview)
            .RequireAuthorization(DwimsPolicies.Reviewer);
        group.MapGet("/{id:guid}", GetSubmission);
        group.MapPost("/{id:guid}/steps/{stepId}", RespondSubmission)
            .RequireAuthorization(DwimsPolicies.Reviewer);
        group.MapDelete("/{id:guid}", DeleteSubmission);
        
        
        return app;
    }

    private static async Task<IResult> CreateSubmission(
        CreateSubmissionRequest request,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        var result = await submissionService.CreateSubmissionAsync(request, cancellationToken);
        return result.ToOkResult();
    }

    private static Task GetMySubmissions(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static Task GetSubmissionsToReview(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static Task GetSubmission(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static async Task<IResult> RespondSubmission(
        Guid submissionId,
        Guid stepId,
        RespondToStepRequest request,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        var result = await submissionService.RespondToStepAsync(
            submissionId, 
            stepId, 
            request, 
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok()
            : Results.UnprocessableEntity(new
            {
                result.Error,
                result.ErrorDescription
            });
    }

    private static Task DeleteSubmission(HttpContext context)
    {
        throw new NotImplementedException();
    }
}