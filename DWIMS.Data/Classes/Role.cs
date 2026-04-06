using DWIMS.Data.Interfaces;

namespace DWIMS.Data;

public class Role : ISoftDeletable
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public ICollection<Document> Documents { get; set; }
    
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
}