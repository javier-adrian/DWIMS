namespace DWIMS.Service.Process.Requests;

public sealed class UpdateProcessRequest
{
    public required string Title { get; set; }
    public string? Description { get; set; }
}