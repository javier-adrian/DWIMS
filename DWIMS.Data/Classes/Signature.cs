namespace DWIMS.Data;

public class Signature
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
    
    public required string MimeType { get; set; }
    public required byte[] EncryptedBlob { get; set; }
    
    public DateTime Created { get; set; }
    public bool isCurrent { get; set; } = true;
}