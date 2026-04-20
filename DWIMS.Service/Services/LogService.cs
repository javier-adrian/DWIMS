using DWIMS.Data;
using DWIMS.Service.Common;
using DWIMS.Service.Logs;
using DWIMS.Service.Logs.Dtos;
using DWIMS.Service.Logs.Requests;
using Microsoft.EntityFrameworkCore;

namespace DWIMS.Service.Services;

public class LogService(AppDbContext context) : ILogService
{
    public async Task<PagedResult<LogDto>> GetLogsAsync(LogFilterRequest request, CancellationToken cancellationToken = default)
    {
        var query = context.Logs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.ActionFilter))
            query = query.Where(l => l.Action.Contains(request.ActionFilter));

        if (!string.IsNullOrWhiteSpace(request.UserIdFilter) && Guid.TryParse(request.UserIdFilter, out var userId))
            query = query.Where(l => l.UserId == userId);

        if (request.From.HasValue)
            query = query.Where(l => l.Timestamp >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(l => l.Timestamp <= request.To.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var logs = await query
            .Include(l => l.User)
            .OrderByDescending(l => l.Timestamp)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(l => new LogDto(
                l.Id,
                l.UserId ?? Guid.Empty,
                l.User != null ? $"{l.User.FirstName} {l.User.MiddleName} {l.User.LastName}" : null,
                l.Action,
                l.EntityType,
                l.EntityId,
                l.Timestamp,
                l.Metadata,
                l.IpAddress))
            .ToListAsync(cancellationToken);

        return new PagedResult<LogDto>(logs, totalCount, request.Page, request.PageSize);
    }
}