using DWIMS.Data;
using DWIMS.Service.Common;
using DWIMS.Service.Submission.Dtos;
using DWIMS.Service.Submission.Requests;

namespace DWIMS.Service.Submission;

public interface ISubmissionService
{
    Task<Result<IReadOnlyList<SubmissionSummaryDto>>> GetMySubmissionsAsync(
        Status? statusFilter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    
    Task<Result<IReadOnlyList<PendingReviewDto>>> GetPendingReviewAsync(
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

    Task<Result> DeleteSubmissionAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}