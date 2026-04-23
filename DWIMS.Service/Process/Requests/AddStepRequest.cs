using DWIMS.Data;

namespace DWIMS.Service.Process.Requests;

public sealed class AddStepRequest
{
    public required string Title { get; set; }
    public Guid? DepartmentId { get; set; }
    public required int Order { get; set; }
    public required GeneralRole Role { get; set; }
}