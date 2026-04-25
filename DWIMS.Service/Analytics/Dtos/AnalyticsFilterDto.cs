namespace DWIMS.Service.Analytics.Dtos;

public sealed record AnalyticsFilterDto
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? ProcessId { get; set; }
}