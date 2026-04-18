using DWIMS.Data.Interfaces;

namespace DWIMS.Data;

public class Step : ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid? FieldId { get; set; }
    public Guid? ProcessId { get; set; }
    public Guid DepartmentId { get; set; }
    public int Order { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }

    public GeneralRole Role { get; set; } = GeneralRole.Reviewer;
    public Department Department { get; set; }
    public Field Field { get; set; }
    public Process Process { get; set; }
    
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
}