using DWIMS.Data.Interfaces;

namespace DWIMS.Data;

public class Response : ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public Submission Submission { get; set; }
    public Guid StepId { get; set; }
    public Step Step { get; set; }
    public Guid? ReviewerId { get; set; }
    public User Reviewer { get; set; }
    public Status Result { get; set; }
    public string Remarks { get; set; }
    public DateTime ActivatedOn { get; set; }
    public DateTime CompletedOn { get; set; }
    
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
}