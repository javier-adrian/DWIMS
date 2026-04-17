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
        group.MapGet("/review", GetSubmissionsToReview);
        group.MapGet("/{id:guid}", GetSubmission);
        group.MapPost("/{id:guid}/steps/{stepId}", RespondSubmission);
        group.MapDelete("/{id:guid}", DeleteSubmission);
        
        
        return app;
    }

    private static Task CreateSubmission(HttpContext context)
    {
        throw new NotImplementedException();
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

    private static Task RespondSubmission(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static Task DeleteSubmission(HttpContext context)
    {
        throw new NotImplementedException();
    }
}