namespace DWIMS.Data;

public class SubmissionRoute
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public Submission Submission { get; set; }
    public Guid StepId { get; set; }
    public Step Step { get; set; }
    public Guid AssigneeId { get; set; }
    public User Assignee { get; set; }
}