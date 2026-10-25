using DWIMS.Data;

namespace DWIMS.Service.Process.Requests;

public sealed class CreateProcessRequest
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? ControlNumber { get; set; }
    public ProcessKind Kind { get; set; }
}