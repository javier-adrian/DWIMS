using DWIMS.Data.Interfaces;

namespace DWIMS.Data;

public class User : ISoftDeletable
{
    public Guid Id { get; set; }
    
    public string FirstName { get; set; }
    public string MiddleName { get; set; }
    public string LastName { get; set; }
    
    public DateTime BirthDate { get; set; }
    public string Email { get; set; }
    public string? ContactNumber { get; set; }
    
    public string? Password { get; set; }

    public ICollection<Role> Roles { get; set; } = [];
    public ICollection<Department> Departments { get; set; }
    public ICollection<ExternalLogin> Logins { get; set; }
    
    public Signature? Signature { get; set; }
    public ICollection<Submission> Submissions { get; set; }
    
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
}