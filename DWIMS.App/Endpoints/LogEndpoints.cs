using DWIMS.Service.Auth;
using DWIMS.Service.Logs;
using DWIMS.Service.Logs.Requests;
using DWIMS.Service.Services;

namespace DWIMS.Controllers;

public static class LogEndpoints
{
    public static IEndpointRouteBuilder MapLogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("logs")
            .WithTags("Logs")
            .RequireAuthorization(DwimsPolicies.Administrator);

        group.MapGet("/", GetLogs)
            .WithDisplayName("Get Logs")
            .WithSummary("Get logs with filtering and pagination");

        return app;
    }

    private static async Task<IResult> GetLogs(
        [AsParameters] LogFilterRequest request,
        ILogService logService,
        CancellationToken cancellationToken)
    {
        var result = await logService.GetLogsAsync(request, cancellationToken);
        return Results.Ok(result);
    }
}
