using System.Globalization;
using DWIMS.Data.Interfaces;

namespace DWIMS.Data;

public class Document : ISoftDeletable
{
    public Guid Id { get; set; }
    public string Link { get; set; }
    public Guid ProcessId { get; set; }
    public Process Process { get; set; }
    public ICollection<Field> Fields { get; set; }
    
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
}