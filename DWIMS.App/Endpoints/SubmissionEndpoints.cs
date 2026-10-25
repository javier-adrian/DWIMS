using System.Text.Json;
using DWIMS.Data;
using DWIMS.Service.Auth;
using DWIMS.Service.Common;
using DWIMS.Service.Process.Dtos;
using DWIMS.Service.Process.Requests;
using DWIMS.Service.Routing.Dtos;
using DWIMS.Service.Services;
using DWIMS.Service.Submission;
using DWIMS.Service.Submission.Dtos;
using DWIMS.Service.Submission.Requests;

namespace DWIMS.Controllers;

public static class SubmissionEndpoints
{
    private static readonly JsonSerializerOptions Json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    
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
            .WithSummary("Get all submissions pending review");
            // .RequireAuthorization(DwimsPolicies.Reviewer);
        group.MapGet("/reviewed", GetSubmissionsReviewed)
            .WithDisplayName("Get Reviewed Submissions")
            .WithSummary("Get all submissions reviewed");
        group.MapGet("/all", GetAllSubmissions)
            .WithDisplayName("Get All Submissions")
            .WithSummary("Get all submissions");
            // .RequireAuthorization(DwimsPolicies.Reviewer);
        group.MapGet("/review/{id:guid}", GetSubmissionToReview)
            .WithDisplayName("Get Submission to Review")
            .WithSummary("Get a submission's full details for review");
            // .RequireAuthorization(DwimsPolicies.Reviewer);
        group.MapGet("/{id:guid}", GetSubmission)
            .WithDisplayName("Get Submission")
            .WithSummary("Get a submission by ID");
        group.MapPost("/{id:guid}/steps/{stepId}", RespondSubmission)
            .WithDisplayName("Respond to Step")
            .WithSummary("Approve or reject a submission at a specific step");
            // .RequireAuthorization(DwimsPolicies.Reviewer);
        group.MapPost("/{id:guid}/cancel", CancelSubmission)
            .WithDisplayName("Cancel Submission")
            .WithSummary("Cancel a submission");
        group.MapPost("/{id:guid}/resume", ResumeSubmission)
            .WithDisplayName("Resume Submission")
            .WithSummary("Resume a submission");
        group.MapPost("/{id:guid}/redirect", RedirectSubmission)
            .WithDisplayName("Redirect Submission")
            .WithSummary("Redirect a submission to another reviewer");
        group.MapGet("/{id:guid}/pdf", GetDocument)
            .WithDisplayName("Get Result Document")
            .WithSummary("Get the result document of a submission");
        group.MapGet("/{id:guid}/attachments", GetAttachments)
            .WithDisplayName("Get Submission Attachments")
            .WithSummary("Get a submission's attachments");
        
        group.MapGet("/{id:guid}/attachments/{attachmentId:guid}", DownloadAttachment)
            .WithDisplayName("Download Attachment")
            .WithSummary("Download a submission's attachment");
        
        group.MapPost("/{id:guid}/attachments", UploadAttachment)
            .WithDisplayName("Upload Attachment")
            .WithSummary("Upload attachments to a submission.")
            .DisableAntiforgery();
        
        group.MapDelete("/{id:guid}/attachments/{attachmentId:guid}", DeleteAttachment)
            .WithDisplayName("Delete Attachment")
            .WithSummary("Delete a submission's attachment.");
        
        group.MapPost("/document", CreateDocumentSubmission)
            .WithDisplayName("Create Document Submission")
            .WithSummary("Submit a letter or a memorandum.")
            .DisableAntiforgery();
        
        group.MapGet("/{id:guid}/source", DownloadDocumentSubmission)
            .WithDisplayName("Get Document Submission")
            .WithSummary("Get source document of a submission.")
            .DisableAntiforgery();
        
        return app;
    }

    private static async Task<IResult> DownloadDocumentSubmission(
        Guid id,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        var result = await submissionService.DownloadDocumentAsync(id, cancellationToken);
        if (!result.IsSuccess)
            return Results.UnprocessableEntity(new
            {
                result.Error
            });
        var dto = result.Data!;
        return Results.File(dto.Content, dto.ContentType, dto.FileName);
    }

    private static async Task<IResult> CreateDocumentSubmission(
        HttpRequest http,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        var form = await http.ReadFormAsync(cancellationToken);
        
        string? processId = form["processId"].ToString();
        IFormFile? file = form.Files.FirstOrDefault(f => f.Name == "file");
        string? routeJson = form["route"].ToString();
        string? slotsJson = form["slots"].ToString();

        IReadOnlyList<RouteEntryDto> route;
        IReadOnlyList<SlotEntryDto> slots;

        try
        {
            route = JsonSerializer.Deserialize<IReadOnlyList<RouteEntryDto>>(routeJson, Json);
        }
        catch (Exception e)
        {
            return Results.UnprocessableEntity(new {Error = "INVALID_BODY", ErrorMessage = "Unprocessable body content."});
        }
        
        try
        {
            slots = JsonSerializer.Deserialize<IReadOnlyList<SlotEntryDto>>(slotsJson, Json);
        }
        catch (Exception e)
        {
            return Results.UnprocessableEntity(new {Error = "INVALID_BODY", ErrorMessage = "Unprocessable body content."});
        }
        
        var request = new CreateDocumentSubmissionRequest(processId, file, route, slots);

        var result = await submissionService.CreateDocumentSubmissionAsync(request, cancellationToken);

        return result.ToOkResult();
    }

    private static async Task<IResult> RedirectSubmission(
        Guid id,
        RedirectSubmissionRequest request,
        ISubmissionService submissionService, 
        CancellationToken cancellationToken)
    {
        var result = await submissionService.RedirectSubmissionAsync(id, request, cancellationToken);
        
        return result.IsSuccess
            ? Results.Ok()
            : Results.UnprocessableEntity(new
            {
                result.Error,
                result.ErrorDescription
            });
    }

    private static async Task<IResult> GetSubmissionsReviewed(
        [AsParameters] GetMySubmissionsQuery query,
        ISubmissionService submissionService, 
        CancellationToken cancellationToken)
    {
        var result = await submissionService.GetSubmissionsReviewedAsync(query.ProcessId, query.Page, query.PageSize, cancellationToken);
        return result.IsSuccess
            ? Results.Ok(result.Data)
            : Results.UnprocessableEntity(new
            {
                result.Error,
                result.ErrorDescription
            });
    }
    
    private static async Task<IResult> GetAllSubmissions(
        [AsParameters] SubmissionFilterRequest request,
        ISubmissionService submissionService, 
        CancellationToken cancellationToken)
    {
        var result = await submissionService.GetAllSubmissionsAsync(request, cancellationToken);
        
        return result.ToOkResult();
    }

    private static async Task<IResult> ResumeSubmission(
        Guid id,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        var result = await submissionService.ResumeSubmissionAsync(id, cancellationToken);
        
        return result.IsSuccess
            ? Results.Ok()
            : Results.UnprocessableEntity(new
            {
                result.Error,
                result.ErrorDescription
            });
    }

    private static async Task<IResult> DeleteAttachment(
        Guid id, 
        Guid attachmentId, 
        ISubmissionService submissionService, 
        CancellationToken cancellationToken)
    {
        var result = await submissionService.DeleteAttachmentAsync(id, attachmentId, cancellationToken);
        return result.IsSuccess
            ? Results.NoContent()
            : Results.UnprocessableEntity(new
            {
                result.Error
            });
    }

    private static async Task<IResult> UploadAttachment(
        Guid id, 
        IFormFileCollection files, 
        ISubmissionService submissionService, 
        CancellationToken cancellationToken)
    {
        List<AttachmentUploadDto> attachments = [];
        foreach (var file in files)
        {
            var dto = new AttachmentUploadDto(file.OpenReadStream(), file.FileName, file.ContentType);
            attachments.Add(dto);
        }
        
        var result = await submissionService.UploadAttachmentsAsync(id, attachments, cancellationToken);
        return result.IsSuccess
            ? Results.NoContent()
            : Results.UnprocessableEntity(new
            {
                result.Error
            });
    }

    private static async Task<IResult> DownloadAttachment(
        Guid id,
        Guid attachmentId,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        var result = await submissionService.DownloadAttachmentAsync(id, attachmentId, cancellationToken);
        if (!result.IsSuccess)
            return Results.UnprocessableEntity(new
            {
                result.Error
            });
        var dto = result.Data!;
        return Results.File(dto.Content, dto.ContentType, dto.FileName);
    }

    private static async Task<IResult> GetAttachments(
        Guid id,
        ISubmissionService submissionService,
        CancellationToken cancellationToken)
    {
        var result = await submissionService.GetAttachmentsAsync(id, cancellationToken);
        
        return result.IsSuccess
            ? Results.Ok(result.Data)
            : Results.UnprocessableEntity(new
            {
                result.Error
            });
    }

    private static async Task<IResult> GetDocument(
        Guid id,
        IPdfGenerationService pdfGenerationService,
        CancellationToken cancellationToken)
    {

        try
        {
            var stream = await pdfGenerationService.GenerateAsync(id, cancellationToken);
            return Results.File(stream, "application/pdf");
        }
        catch (UnauthorizedAccessException e)
        {
            return Results.Forbid();
        }
        catch (KeyNotFoundException e)
        {
            return Results.NotFound();
        }
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
        var result = await submissionService.GetMySubmissionsAsync(
            query.Status, 
            query.ProcessId, 
            query.Page, 
            query.PageSize, 
            cancellationToken);
        
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