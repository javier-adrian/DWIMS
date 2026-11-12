using DWIMS.Service.Analysis;

namespace DWIMS.Service.Process.Dtos;

public sealed record TemplateMappingDto(
    int MappingState,
    TemplateAnalysis? Suggestions,
    IReadOnlyList<FieldDto> Fields,
    IReadOnlyList<StepDto> Steps);