namespace DWIMS.Data;

public class PasswordResetToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Token { get; set; }
    public DateTime Expires { get; set; }
    public bool Used { get; set; }
    public DateTime Created { get; set; }
    public User User { get; set; }
}