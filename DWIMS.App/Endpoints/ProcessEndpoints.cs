using DWIMS.Service.Auth;
using DWIMS.Service.Process;
using DWIMS.Service.Process.Requests;
using DWIMS.Service.Services;

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

    private static async Task<IResult> CreateProcess(
        CreateProcessRequest request,
        IProcessService departmentService,
        CancellationToken cancellationToken)
    {
        var result = await departmentService.CreateProcessAsync(request, cancellationToken);
        return result.ToCreatedResult("/process/");
    }

    private static async Task GetProcesses(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static async Task GetProcess(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static async Task UpdateProcess(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static async Task DeleteProcess(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static async Task<IResult> CreateStep(
        Guid processId,
        AddStepRequest request,
        IProcessService departmentService,
        CancellationToken cancellationToken)
    {
        var result = await departmentService.AddStepAsync(processId, request, cancellationToken);
        return result.ToCreatedResult("/process/step/");
    }

    private static async Task GetSteps(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static async Task GetStep(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static async Task UpdateStep(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static async Task DeleteStep(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static async Task CreateField(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static async Task GetFields(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static async Task GetField(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static async Task UpdateField(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static async Task DeleteField(HttpContext context)
    {
        throw new NotImplementedException();
    }
}