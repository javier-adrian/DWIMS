namespace DWIMS.Service.Analytics.Dtos;

public sealed record StepFunnelDto
{
    public required Guid ProcessId { get; init; }
    public required string ProcessName { get; init; }
    public required IReadOnlyList<StepFunnelEntryDto> Steps { get; init; }
}