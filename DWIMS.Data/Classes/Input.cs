using DWIMS.Data.Interfaces;

namespace DWIMS.Data;

public class Input : ISoftDeletable
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Value { get; set; }
    public InputTypes Type { get; set; }
    public bool Required { get; set; } = true;
    
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
}