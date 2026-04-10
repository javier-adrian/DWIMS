using DWIMS.Data.Interfaces;

namespace DWIMS.Data;

public class Document : ISoftDeletable
{
    public int Id { get; set; }
    public string Link { get; set; }
    
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
}