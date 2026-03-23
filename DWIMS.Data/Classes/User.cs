namespace DWIMS.Data;

public class User
{
    public int Id { get; set; }
    
    public string FirstName { get; set; }
    public string MiddleName { get; set; }
    public string LastName { get; set; }
    
    public int BirthDate { get; set; }
    public int Email { get; set; }
    public int ContactNumber { get; set; }
    
    public GeneralRole GeneralRole { get; set; }
    public ICollection<Role> SpecificRoles { get; set; }
    public ICollection<Department> Departments { get; set; }
    
    public Signature Signature { get; set; }
    public ICollection<Submission> Submissions { get; set; }
}