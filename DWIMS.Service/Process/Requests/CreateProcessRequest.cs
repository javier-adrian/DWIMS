namespace DWIMS.Service.Process.Requests;

public sealed class CreateProcessRequest
{
    public required Guid DepartmentId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
}