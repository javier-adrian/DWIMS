using DWIMS.Service.Analytics;
using DWIMS.Service.Analytics.Dtos;
using DWIMS.Service.Auth;
using DWIMS.Service.Services;

namespace DWIMS.Controllers;

public static class AnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("analytics")
            .WithTags("Analytics")
            .RequireAuthorization(DwimsPolicies.Administrator);

        group.MapGet("/summary", GetSummary)
            .WithDisplayName("Get Summary")
            .WithSummary("Get summary of department statistics");

        return app;
    }

    private static async Task<IResult> GetSummary(
        [AsParameters] AnalyticsFilterDto filter,
        IAnalyticsService analyticsService,
        CancellationToken cancellationToken)
    {
        var result = await analyticsService.GetSummaryAsync(filter, cancellationToken);
        return result.ToOkResult();
    }
}