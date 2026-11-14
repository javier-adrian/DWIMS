namespace DWIMS.Service.Analytics.Dtos;

public sealed record ApprovalSpeedDto
{
    public required IReadOnlyList<ApprovalWindowDto> Windows { get; init; }
    public int ApprovedCount { get; init; }
    public double MedianMinutes { get; init; }
    public double P90Minutes { get; init; }
    public int ApproveResponses { get; init; }
    public double ApproveMedianMinutes { get; init; }
    public int RejectResponses { get; init; }
    public double RejectMedianMinutes { get; init; }
}