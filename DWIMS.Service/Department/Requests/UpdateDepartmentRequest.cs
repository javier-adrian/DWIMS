namespace DWIMS.Service.Department.Requests;

public sealed class UpdateDepartmentRequest
{
    public required string Name { get; set; }
    public string? Description { get; set; }
}