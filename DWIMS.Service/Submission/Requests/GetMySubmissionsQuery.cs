using DWIMS.Data;

namespace DWIMS.Service.Submission.Requests;

public sealed record GetMySubmissionsQuery(
    Status? Status = null,
    int Page = 1,
    int PageSize = 10);
