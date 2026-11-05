namespace DWIMS.Service.Analytics.Dtos;

public sealed record StepFunnelEntryDto(
    Guid StepId,
    string StepName,
    int StepOrder,
    double AverageMinutes,
    int ResponseCount);