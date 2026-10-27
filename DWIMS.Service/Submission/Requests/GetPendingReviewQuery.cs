namespace DWIMS.Service.Submission.Requests;

public sealed record GetPendingReviewQuery(
    int Page = 1, 
    int PageSize = 100);