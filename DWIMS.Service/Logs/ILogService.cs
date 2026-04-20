using DWIMS.Service.Common;
using DWIMS.Service.Logs.Dtos;
using DWIMS.Service.Logs.Requests;

namespace DWIMS.Service.Logs;

public interface ILogService
{
    Task<PagedResult<LogDto>> GetLogsAsync(
        LogFilterRequest request,
        CancellationToken cancellationToken = default);

    Task LogAsync(
        string action,
        string? entityType = null,
        Guid? entityId = null,
        string? metadata = null,
        CancellationToken cancellationToken = default);
}