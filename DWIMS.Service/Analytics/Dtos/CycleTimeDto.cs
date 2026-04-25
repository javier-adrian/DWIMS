namespace DWIMS.Service.Analytics.Dtos;

public sealed record CycleTimeDto
{
    public double AverageCycleTime { get; init; }
    public double MinCycleTime { get; init; }
    public double MaxCycleTime { get; init; }
    public required IReadOnlyList<DailyAverageDto> ByDay { get; init; }
}