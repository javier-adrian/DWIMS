using DWIMS.Data.Interfaces;

namespace DWIMS.Data;

public class Input : ISoftDeletable
{
    public Guid Id { get; set; }
    public string Value { get; set; }
    public Guid SubmissionId { get; set; }
    public Submission Submission { get; set; }
    public Guid FieldId { get; set; }
    public Field Field { get; set; }
    
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
}