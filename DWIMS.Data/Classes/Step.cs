using DWIMS.Data.Interfaces;

namespace DWIMS.Data;

public class Step : ISoftDeletable
{
    public int Id { get; set; }
    public int Order { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    
    public Role Role { get; set; }
    public Department Department { get; set; }
    public Field Field { get; set; }
    public Process Process { get; set; }
    
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
}