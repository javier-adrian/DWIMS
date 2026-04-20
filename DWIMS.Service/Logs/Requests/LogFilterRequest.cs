namespace DWIMS.Service.Logs.Requests;

public sealed class LogFilterRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    
    public string? ActionFilter { get; set; }
    public string? UserIdFilter { get; set; }
    
    public DateTime? From{ get; set; }
    public DateTime? To{ get; set; }
}