using DWIMS.Service.Analytics.Dtos;
using DWIMS.Service.Common;

namespace DWIMS.Service.Analytics;

public interface IAnalyticsService
{
        Task<Result<AnalyticsSummaryDto>> GetSummaryAsync(
                AnalyticsFilterDto filter, 
                CancellationToken cancellationToken = default);
}