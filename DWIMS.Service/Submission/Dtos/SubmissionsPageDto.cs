namespace DWIMS.Service.Submission.Dtos;

public record SubmissionsPageDto(
    IReadOnlyList<SubmissionSummaryDto> Items,
    IReadOnlyList<SubmissionsCategoryDto> Categories,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}