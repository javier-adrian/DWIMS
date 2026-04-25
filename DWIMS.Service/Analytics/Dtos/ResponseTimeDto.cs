namespace DWIMS.Service.Analytics.Dtos;

public sealed record ResponseTimeDto
{
    public double AverageResponseTime { get; init; }
    public double MinResponseTime { get; init; }
    public double MaxResponseTime { get; init; }
    public required IReadOnlyList<StepResponseTimeDto> ByStep { get; init; }
}