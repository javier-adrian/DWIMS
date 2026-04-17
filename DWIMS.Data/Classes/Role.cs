using DWIMS.Data.Interfaces;

namespace DWIMS.Data;

public class Role : ISoftDeletable
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public GeneralRole GeneralRole { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public ICollection<Document> Documents { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
    
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
}