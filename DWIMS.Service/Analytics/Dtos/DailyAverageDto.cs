namespace DWIMS.Service.Analytics.Dtos;

public sealed record DailyAverageDto(
    DateOnly Date,
    double Average);