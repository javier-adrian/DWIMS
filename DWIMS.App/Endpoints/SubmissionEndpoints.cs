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

        group.MapPost("/", CreateSubmission)
            .WithDisplayName("Create Submission")
            .WithSummary("Submit a new document for review");
        group.MapGet("/own", GetMySubmissions)
            .WithDisplayName("Get My Submissions")
            .WithSummary("Get the current user's submissions");
        group.MapGet("/review", GetSubmissionsToReview)
            .WithDisplayName("Get Pending Reviews")
            .WithSummary("Get all submissions pending review")
            .RequireAuthorization(DwimsPolicies.Reviewer);
        group.MapGet("/review/{id:guid}", GetSubmissionToReview)
            .WithDisplayName("Get Submission to Review")
            .WithSummary("Get a submission's full details for review")
            .RequireAuthorization(DwimsPolicies.Reviewer);
        group.MapGet("/{id:guid}", GetSubmission)
            .WithDisplayName("Get Submission")
            .WithSummary("Get a submission by ID");
        group.MapPost("/{id:guid}/steps/{stepId}", RespondSubmission)
            .WithDisplayName("Respond to Step")
            .WithSummary("Approve or reject a submission at a specific step")
            .RequireAuthorization(DwimsPolicies.Reviewer);
        group.MapPost("/{id:guid}/cancel", CancelSubmission)
            .WithDisplayName("Cancel Submission")
            .WithSummary("Cancel a submission");
        
        
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

    private static async Task<IResult> GetMySubmissions(
        [AsParameters] GetMySubmissionsQuery query,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        var result = await submissionService.GetMySubmissionsAsync(query.Status, query.Page, query.PageSize, cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Data)
            : Results.UnprocessableEntity(new
            {
                result.Error,
                result.ErrorDescription
            });
    }

    private static async Task<IResult> GetSubmissionsToReview(
        ISubmissionService submissionService,
        CancellationToken cancellationToken
        )
    {
        var result = await submissionService.GetPendingReviewAsync(cancellationToken);
        return result.ToOkResult();
    }

    private static async Task<IResult> GetSubmissionToReview(
        Guid id, 
        ISubmissionService submissionService, 
        CancellationToken cancellationToken)
    {
        var result = await submissionService.GetPendingReviewAsync(id, cancellationToken);
        return result.ToOkResult();
    }

    private static async Task<IResult> GetSubmission(
        Guid id,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        var result = await submissionService.GetSubmissionAsync(id, cancellationToken);
        return result.ToOkResult();
    }

    private static async Task<IResult> RespondSubmission(
        Guid id,
        Guid stepId,
        RespondToStepRequest request,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        var result = await submissionService.RespondToStepAsync(
            id,
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

    private static async Task<IResult> CancelSubmission(
        Guid id,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        var result = await submissionService.CancelSubmissionAsync(id, cancellationToken);
        return result.IsSuccess
            ? Results.Ok()
            : Results.UnprocessableEntity(new
            {
                result.Error,
                result.ErrorDescription
            });
    }
}