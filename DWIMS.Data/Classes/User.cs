using DWIMS.Data.Interfaces;

namespace DWIMS.Data;

public class User : ISoftDeletable
{
    public int Id { get; set; }
    
    public string FirstName { get; set; }
    public string MiddleName { get; set; }
    public string LastName { get; set; }
    
    public DateTime BirthDate { get; set; }
    public string Email { get; set; }
    public string ContactNumber { get; set; }
    
    public GeneralRole GeneralRole { get; set; }
    public ICollection<Role> SpecificRoles { get; set; }
    public ICollection<Department> Departments { get; set; }
    
    public Signature Signature { get; set; }
    public ICollection<Submission> Submissions { get; set; }
    
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
}