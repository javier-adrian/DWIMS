using DWIMS.Data.Interfaces;

namespace DWIMS.Data;

public class Process : ISoftDeletable
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string? ControlNumber { get; set; }
    public string? Description { get; set; }
    public int LastIssuedSerial { get; set; }
    public ProcessKind Kind { get; set; }
    public ICollection<Step> Steps { get; set; }
    public ICollection<Field> Fields { get; set; }
    public ICollection<Document> Documents { get; set; }
    public bool IsActive { get; set; } = true;
    
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
}