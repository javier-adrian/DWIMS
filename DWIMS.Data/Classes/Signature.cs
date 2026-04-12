namespace DWIMS.Data;

public class Signature
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
    public string Link { get; set; }
}