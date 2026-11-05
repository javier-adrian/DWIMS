namespace DWIMS.Service.Analytics.Dtos;

public sealed record AnalyticsSummaryDto
{
    public required ResponseTimeDto ResponseTime { get; init; }
    public required VolumeDto Volume { get; init; }
    public required CycleTimeDto CycleTime { get; init; }
    public required IReadOnlyList<ReviewerPerformanceDto> Reviewers { get; init; }
    public required PendingAgingDto PendingAging { get; init; }
    public required StepFunnelDto StepFunnel { get; init; }
}