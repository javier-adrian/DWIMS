namespace DWIMS.Service.Analytics.Dtos;

public sealed record LeadTimeDto
{
    public double AverageLeadTime { get; init; }
    public double MinLeadTime { get; init; }
    public double MaxLeadTime { get; init; }
    public required IReadOnlyList<DailyAverageDto> ByDay { get; init; }
}