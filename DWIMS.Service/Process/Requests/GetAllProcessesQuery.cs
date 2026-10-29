namespace DWIMS.Service.Process.Requests;

public sealed record GetAllProcessesQuery(
    int Page = 1,
    int PageSize = 100,
    string? Query = null);