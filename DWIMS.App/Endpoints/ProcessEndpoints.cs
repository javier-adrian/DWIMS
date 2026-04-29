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
            .WithDisplayName("Create Process")
            .WithSummary("Create a new process in a department")
            .RequireAuthorization(DwimsPolicies.Administrator);
        group.MapGet("/", GetProcesses)
            .WithDisplayName("Get Processes")
            .WithSummary("Get all processes");
        group.MapGet("/all", GetAllProcesses)
            .WithDisplayName("Get All Processes")
            .WithSummary("Get all processes without department filtering");
        group.MapGet("/{id:guid}", GetProcess)
            .WithDisplayName("Get Process")
            .WithSummary("Get a process by ID with steps and fields");
        group.MapPut("/{id:guid}", UpdateProcess)
            .WithDisplayName("Update Process")
            .WithSummary("Update an existing process")
            .RequireAuthorization(DwimsPolicies.Administrator);
        group.MapDelete("/{id:guid}", DeleteProcess)
            .WithDisplayName("Delete Process")
            .WithSummary("Delete a process")
            .RequireAuthorization(DwimsPolicies.Administrator);
        
        group.MapPost("/{id:guid}/document", CreateDocument)
            .WithDisplayName("Add Document")
            .WithSummary("Add a document to a process")
            .RequireAuthorization(DwimsPolicies.Administrator);

        group.MapPost("/{id:guid}/step", CreateStep)
            .WithDisplayName("Add Step")
            .WithSummary("Add a step to a process")
            .RequireAuthorization(DwimsPolicies.Administrator);
        group.MapGet("/{id:guid}/step", GetSteps)
            .WithDisplayName("Get Steps")
            .WithSummary("Get all steps of a process");
        // group.MapGet("/{id:guid}/step/{id:guid}", GetStep);
        group.MapPut("/{processId:guid}/step/{stepId:guid}", UpdateStep)
            .RequireAuthorization(DwimsPolicies.SuperAdministrator);
        // group.MapDelete("/{id:guid/step/{id:guid}", DeleteStep)
        //     .RequireAuthorization(DwimsPolicies.Administrator);
        
        group.MapPost("/{id:guid}/field", CreateField)
            .WithDisplayName("Add Field")
            .WithSummary("Add a field to a process")
            .RequireAuthorization(DwimsPolicies.Administrator);
        group.MapGet("/{id:guid}/field", GetFields)
            .WithDisplayName("Get Fields")
            .WithSummary("Get all fields of a process");
        // group.MapGet("/{id:guid}/field/{id:guid}", GetField);
        // group.MapPut("/{id:guid}/field/{id:guid}", UpdateField)
        //     .RequireAuthorization(DwimsPolicies.Administrator);
        // group.MapDelete("/{id:guid/field/{id:guid", DeleteField)
        //     .RequireAuthorization(DwimsPolicies.Administrator);
        
        return app;
    }

    private static async Task<IResult> CreateDocument(
        Guid id,
        HttpRequest request,
        IProcessService processService,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
            return Results.BadRequest(new { Error = "INVALID_CONTENT_TYPE", ErrorDescription = "Expected multipart/form-data." });

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.FirstOrDefault();

        if (file is null || file.Length == 0)
            return Results.BadRequest(new { Error = "NO_FILE", ErrorDescription = "A document file is required." });

        if (!file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(new { Error = "INVALID_FILE_TYPE", ErrorDescription = "Only PDF files are allowed." });

        var uploadRequest = new UploadDocumentRequest
        {
            File = file.OpenReadStream(),
            FileName = file.FileName,
        };

        var result = await processService.UploadDocumentAsync(id, uploadRequest, cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : Results.UnprocessableEntity(new { result.Error, result.ErrorDescription });
    }

    private static async Task<IResult> CreateProcess(
        CreateProcessRequest request,
        IProcessService departmentService,
        CancellationToken cancellationToken)
    {
        var result = await departmentService.CreateProcessAsync(request, cancellationToken);
        return result.ToCreatedResult("/process/");
    }

    private static async Task<IResult> GetProcesses(
        IProcessService processService,
        CancellationToken cancellationToken
        )
    {
        var result = await processService.GetProcessesAsync(cancellationToken);
        return result.ToOkResult();
    }

    private static async Task<IResult> GetAllProcesses(
        IProcessService processService,
        CancellationToken cancellationToken)
    {
        var result = await processService.GetAllProcessesAsync(cancellationToken);
        return result.ToOkResult();
    }

    private static async Task<IResult> GetProcess(
        Guid id,
        IProcessService processService,
        CancellationToken cancellationToken)
    {
        var result = await processService.GetProcessAsync(id, cancellationToken);
        return result.ToOkResult();
    }

    private static async Task UpdateProcess(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static async Task<IResult> DeleteProcess(
        Guid id,
        IProcessService processService,
        CancellationToken cancellationToken)
    {
        var result = await processService.DeleteProcessAsync(id, cancellationToken);
        return result.IsSuccess
            ? Results.NoContent()
            : Results.UnprocessableEntity(new { result.Error, result.ErrorDescription });
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

    private static async Task<IResult> GetSteps(
        Guid id,
        IProcessService processService,
        CancellationToken cancellationToken)
    {
        var result = await processService.GetStepsAsync(id, cancellationToken);
        return result.ToOkResult();
    }

    private static async Task GetStep(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static async Task<IResult> UpdateStep(
        Guid processId,
        Guid stepId,
        UpdateStepRequest request,
        IProcessService processService,
        CancellationToken cancellationToken)
    {
        var result = await processService.UpdateStepAsync(processId, stepId, request, cancellationToken);
        return result.IsSuccess
            ? Results.NoContent()
            : Results.UnprocessableEntity(new { result.Error, result.ErrorDescription });
    }

    private static async Task DeleteStep(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static async Task<IResult> CreateField(
        Guid processId,
        AddFieldRequest request, 
        IProcessService processService,
        CancellationToken cancellationToken)
    {
        var result = await processService.AddFieldAsync(processId, request, cancellationToken);
        return result.ToCreatedResult("/process/field/");
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