namespace DWIMS.Data;

public class Notification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public NotificationKind Kind { get; set; }
    public Guid? SubmissionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public DateTime? ReadOn { get; set; }
}