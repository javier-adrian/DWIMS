using DWIMS.Data;
using DWIMS.Service.Common;
using DWIMS.Service.Process.Requests;
using DWIMS.Service.Submission.Dtos;
using DWIMS.Service.Submission.Requests;

namespace DWIMS.Service.Submission;

public interface ISubmissionService
{
    Task<Result<SubmissionsPageDto>> GetMySubmissionsAsync(
        Status? statusFilter,
        Guid? processId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    
    Task<Result<PagedResult<AdminSubmissionDto>>> GetAllSubmissionsAsync(
        SubmissionFilterRequest filter,
        CancellationToken cancellationToken = default);
    
    Task<Result<PagedResult<PendingReviewDto>>> GetPendingReviewAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    
    Task<Result<SubmissionDetailDto>> GetPendingReviewAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<SubmissionDetailDto>> GetSubmissionAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreateSubmissionAsync(
        CreateSubmissionRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> RespondToStepAsync(
        Guid submissionId,
        Guid stepId,
        RespondToStepRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> CancelSubmissionAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<AttachmentDto>>> GetAttachmentsAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default);

    Task<Result<AttachmentDownloadDto>> DownloadAttachmentAsync(
        Guid submissionId,
        Guid attachmentId,
        CancellationToken cancellationToken = default);
    
    Task<Result<List<Attachment>>> UploadAttachmentsAsync(
        Guid submissionId,
        IReadOnlyList<AttachmentUploadDto> files,
        CancellationToken cancellationToken = default);
    
    Task<Result> DeleteAttachmentAsync(
        Guid submissionId,
        Guid attachmentId,
        CancellationToken cancellationToken = default);
    
    Task<Result> ResumeSubmissionAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default);
    
    Task<Result<SubmissionsPageDto>> GetSubmissionsReviewedAsync(
        Guid? processId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    
    Task<Result> RedirectSubmissionAsync(
        Guid submissionId,
        RedirectSubmissionRequest request,
        CancellationToken cancellationToken = default);
    
    Task<Result<PublicSubmissionStatusDto>> GetPublicSubmissionStatusAsync(
        string id,
        CancellationToken cancellationToken = default);
    
    Task<Result<Guid>> CreateDocumentSubmissionAsync(
        CreateDocumentSubmissionRequest request,
        CancellationToken cancellationToken = default);
    
    Task<Result<DocumentDownloadDto>> DownloadDocumentAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default);
}