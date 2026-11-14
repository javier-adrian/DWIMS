using System.Security.Cryptography;
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
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public Guid? OicId { get; set; }
    public bool OicActive { get; set; }
    public DateTime? OicActiveUntilUtc { get; set; }
    public User? Oic { get; set; }
    
    public string? Password { get; set; }

    public ICollection<Role> Roles { get; set; } = [];
    public ICollection<ExternalLogin> Logins { get; set; }
    
    public Signature? Signature { get; set; }
    public ICollection<Submission> Submissions { get; set; }
    public bool EmailVerified { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
    
    public bool IsDeleted { get; set; }
    public DateTime? DeletedOn { get; set; }
    
    public static bool IsOicActiveFor(User? principal, Guid? oicUserId, DateTime utcNow) =>
        principal is not null && 
        principal.OicId is { } oicId && oicId == oicUserId &&
        principal.OicActive &&
        (principal.OicActiveUntilUtc is null || principal.OicActiveUntilUtc > utcNow);
}