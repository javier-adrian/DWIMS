namespace DWIMS.Service.Analytics.Dtos;

public sealed record PendingAgingDto
{
    public required IReadOnlyList<AgingBucketDto> Buckets { get; init; }
    public required IReadOnlyList<PendingItemDto> Oldest { get; init; }
    public int TotalPending { get; init; }
}