using DWIMS.Data.Interfaces;

namespace DWIMS.Data;

public class Department : ISoftDeletable
{
    public int Id { get; set; }
    public string Title { get; set; }
    public ICollection<Document> Documents { get; set; }
    public ICollection<User> Users { get; set; }
    
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
}