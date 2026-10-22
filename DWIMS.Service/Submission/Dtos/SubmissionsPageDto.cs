namespace DWIMS.Service.Submission.Dtos;

public record SubmissionsPageDto(
    IReadOnlyList<SubmissionSummaryDto> Items,
    IReadOnlyList<SubmissionsCategoryDto> Categories);