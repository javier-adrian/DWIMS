using DWIMS.Data.Interfaces;

namespace DWIMS.Data;

public class Response : ISoftDeletable
{
    public Guid Id { get; set; }
    public Submission Submission { get; set; }
    public Step Step { get; set; }
    public User Reviewer { get; set; }
    public Status Result { get; set; }
    public string Remarks { get; set; }
    public DateTime SubmittedOn { get; set; }
    public DateTime CompletedOn { get; set; }
    
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
}