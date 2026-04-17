using DWIMS.Service.Auth;

namespace DWIMS.Controllers;

public static class ProcessEndpoints
{
    public static IEndpointRouteBuilder MapProcessEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("process")
            .WithTags("Process")
            .RequireAuthorization();

        group.MapPost("/", CreateProcess)
            .RequireAuthorization(DwimsPolicies.Administrator);
        group.MapGet("/", GetProcesses);
        group.MapGet("/{id:guid}", GetProcess);
        group.MapPut("/{id:guid}", UpdateProcess)
            .RequireAuthorization(DwimsPolicies.Administrator);
        group.MapDelete("/{id:guid}", DeleteProcess)
            .RequireAuthorization(DwimsPolicies.Administrator);

        group.MapPost("/{id:guid}/step", CreateStep)
            .RequireAuthorization(DwimsPolicies.Administrator);
        group.MapGet("/{id:guid}/step", GetSteps);
        group.MapGet("/{id:guid}/step/{id:guid}", GetStep);
        group.MapPut("/{id:guid}/step/{id:guid}", UpdateStep)
            .RequireAuthorization(DwimsPolicies.Administrator);
        group.MapDelete("/{id:guid/step/{id:guid", DeleteStep)
            .RequireAuthorization(DwimsPolicies.Administrator);
        
        group.MapPost("/{id:guid}/field", CreateField)
            .RequireAuthorization(DwimsPolicies.Administrator);
        group.MapGet("/{id:guid}/field", GetFields);
        group.MapGet("/{id:guid}/field/{id:guid}", GetField);
        group.MapPut("/{id:guid}/field/{id:guid}", UpdateField)
            .RequireAuthorization(DwimsPolicies.Administrator);
        group.MapDelete("/{id:guid/field/{id:guid", DeleteField)
            .RequireAuthorization(DwimsPolicies.Administrator);
        
        return app;
    }

    private static Task CreateProcess(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static Task GetProcesses(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static Task GetProcess(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static Task UpdateProcess(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static Task DeleteProcess(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static Task CreateStep(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static Task GetSteps(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static Task GetStep(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static Task UpdateStep(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static Task DeleteStep(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static Task CreateField(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static Task GetFields(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static Task GetField(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static Task UpdateField(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static Task DeleteField(HttpContext context)
    {
        throw new NotImplementedException();
    }
}