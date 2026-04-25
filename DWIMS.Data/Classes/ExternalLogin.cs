namespace DWIMS.Data;

public class ExternalLogin
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ExternalLoginProvider Provider { get; set; }
    public required string ProviderSubject { get; set; }
    public DateTime Created { get; set; }
    public User User { get; set; }
}