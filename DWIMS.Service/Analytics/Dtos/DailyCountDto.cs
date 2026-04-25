namespace DWIMS.Service.Analytics.Dtos;

public sealed record DailyCountDto(
    DateOnly Date, 
    int Count);