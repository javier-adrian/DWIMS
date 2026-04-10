using DWIMS.Data.Interfaces;

namespace DWIMS.Data;

public class Input : ISoftDeletable
{
    public int Id { get; set; }
    public string Value { get; set; }
    public Submission Submission { get; set; }
    public Field Field { get; set; }
    
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
}