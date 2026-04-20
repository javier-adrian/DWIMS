namespace DWIMS.Data;

public class Log
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public required string Action { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Metadata { get; set; }
    public string? IpAddress { get; set; }
}