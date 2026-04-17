using DWIMS.Data;

namespace DWIMS.Service.Process.Requests;

public sealed class AddFieldRequest
{
    public required Guid ProcessId { get; set; }
    public required string Title { get; set; }
    public required InputTypes InputType { get; set; }
    public bool Required { get; set; } = true;
}