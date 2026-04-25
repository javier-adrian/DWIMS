namespace DWIMS.Service.Analytics.Dtos;

public sealed record ProcessVolumeDto(
    Guid ProcessId,
    string ProcessName,
    int Total,
    int Completed,
    int Pending);