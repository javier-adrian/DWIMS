using DWIMS.Data.Interfaces;

namespace DWIMS.Data;

public class AttachmentRequirement : ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid ProcessId { get; set; }
    public Process Process { get; set; } = null!;
    public string Title { get; set; } = "";
    public int Order { get; set; }
    
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
}