using DWIMS.Data.Interfaces;

namespace DWIMS.Data;

public class Process : ISoftDeletable
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public ICollection<Step> Steps { get; set; }
    public ICollection<Field> Fields { get; set; }
    public Document Document { get; set; }
    
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
}