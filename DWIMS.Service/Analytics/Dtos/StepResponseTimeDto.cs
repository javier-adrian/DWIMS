namespace DWIMS.Service.Analytics.Dtos;

public sealed record StepResponseTimeDto(
    string StepName,
    double AverageResponseTime,
    double MedianResponseTime,
    double MinResponseTime, 
    double MaxResponseTime,
    int ResponseCount);