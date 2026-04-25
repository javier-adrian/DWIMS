namespace DWIMS.Service.Analytics.Dtos;

public sealed record VolumeDto
{
    public int SubmissionCount { get; init; }
    public int ApprovedCount { get; init; }
    public int RejectedCount { get; init; }
    public int PendingCount { get; init; }
    public int CancelledCount { get; init; }
    
    public double ApprovalRate { get; init; }
    
    public required IReadOnlyList<ProcessVolumeDto> ByProcess { get; init; }
    public required IReadOnlyList<DailyCountDto> ByDay { get; init; }
}