using DWIMS.Data;

namespace DWIMS.Service.Process.Dtos;

public sealed record ProcessSummaryDto(
    Guid Id,
    Guid DepartmentId,
    string DepartmentName,
    string Name,
    string? Description,
    int StepsCount,
    bool hasTemplate);