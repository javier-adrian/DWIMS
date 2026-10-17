namespace DWIMS.Service.Submission.Requests;

public sealed class RedirectSubmissionRequest
{
    public required string NewReviewerEmail { get; init; }
}