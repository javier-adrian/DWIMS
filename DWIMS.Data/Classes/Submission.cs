using DWIMS.Data.Interfaces;

namespace DWIMS.Data;

public class Submission : ISoftDeletable
{
    public Guid Id { get; set; }
    public Process Process { get; set; }
    public Step Step { get; set; }
    public Status Status { get; set; }
    public DateTime SubmittedOn { get; set; }
    public DateTime CompletedOn { get; set; }
    public User Submitter { get; set; }
    public ICollection<Input> Inputs { get; set; }
    public ICollection<Response> Responses { get; set; }
    
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
}