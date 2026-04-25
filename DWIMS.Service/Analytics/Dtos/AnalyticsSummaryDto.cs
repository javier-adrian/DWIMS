namespace DWIMS.Service.Analytics.Dtos;

public sealed record AnalyticsSummaryDto
{
    public required ResponseTimeDto ResponseTime { get; init; }
    public required VolumeDto Volume { get; init; }
    public required CycleTimeDto CycleTime { get; init; }
    public required LeadTimeDto LeadTime { get; init; }
}