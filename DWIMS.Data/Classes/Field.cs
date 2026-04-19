using DWIMS.Data.Interfaces;

namespace DWIMS.Data;

public class Field : ISoftDeletable
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public Guid ProcessId { get; set; }
    public Process Process { get; set; }
    public InputTypes Type { get; set; }
    public bool Required { get; set; } = true;
    public Guid? DocumentId { get; set; }
    public Document Document { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
}