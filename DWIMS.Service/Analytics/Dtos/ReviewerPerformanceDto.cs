namespace DWIMS.Service.Analytics.Dtos;

public sealed record ReviewerPerformanceDto(
    Guid ReviewerId,
    string ReviewerName,
    bool IsActive,
    int OpenAssignments,
    int CompletedReviews,
    double AverageResponseMinutes,
    double MedianResponseMinutes,
    double P90ResponseMinutes);