using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace DWIMS.Service.Process.Dtos;

public sealed record FieldDto(
    Guid Id,
    string Name,
    InputType Type,
    bool Required);