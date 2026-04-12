using DWIMS.Data.Interfaces;

namespace DWIMS.Data;

public class Field : ISoftDeletable
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public Process Process { get; set; }
    public InputTypes Type { get; set; }
    public bool Required { get; set; } = true;
    
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
}