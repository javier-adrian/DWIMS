using DWIMS.Data.Interfaces;

namespace DWIMS.Data;

public class EmailVerificationCode
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Code { get; set; }
    public DateTime Expires { get; set; }
    public bool Used { get; set; }
    public int Attempts { get; set; }
    public DateTime Created { get; set; }
    public User User { get; set; }
}