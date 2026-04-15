namespace DWIMS.Service.Auth.Requests;

public sealed class RegisterRequest
{
    public required string FirstName { get; set; }
    public string? MiddleName { get; set; }
    public required string LastName { get; set; }
    
    public required string Email { get; set; }
    public string ContactNumber { get; set; }
    
    public required string Password { get; set; }
}