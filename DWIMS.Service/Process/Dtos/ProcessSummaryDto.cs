using DWIMS.Data;

namespace DWIMS.Service.Process.Dtos;

public sealed record ProcessSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    int StepsCount,
    bool hasTemplate,
    ProcessKind Kind);