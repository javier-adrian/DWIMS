using DWIMS.Service.Analytics.Dtos;
using DWIMS.Service.Common;

namespace DWIMS.Service.Analytics;

public interface IAnalyticsService
{
    Task<Result<AnalyticsFilterDto>> GetSummaryAsync(
        AnalyticsFilterDto filter,
        CancellationToken cancellationToken = default);
}