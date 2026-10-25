using DWIMS.Data;

namespace DWIMS.Service.Submission.Requests;

public sealed class SubmissionFilterRequest
{
    public Status? Status { get; set; }
    public Guid? ProcessId { get; set; }
    public Guid? SubmitterId { get; set; }
    public Guid? AssigneeId { get; set; }
    public string? Search{ get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}