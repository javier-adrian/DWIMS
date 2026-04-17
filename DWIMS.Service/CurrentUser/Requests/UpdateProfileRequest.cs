namespace DWIMS.Service.CurrentUser.Requests;

public class UpdateProfileRequest
{
    public required string FirstName { get; set; }
    public string? MiddleName { get; set; }
    public required string LastName { get; set; }
    
    public required string Email { get; set; }
    public string? ContactNumber { get; set; }
    public DateTime? BirthDate { get; set; }
}