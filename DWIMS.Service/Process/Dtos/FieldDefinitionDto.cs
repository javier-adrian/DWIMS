using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace DWIMS.Service.Process.Dtos;

public sealed record FieldDefinitionDto(
    string Title,
    InputType Type,
    bool Required);