namespace DWIMS.Service.Analysis;

public sealed record TemplateAnalysis(
    int PageCount,
    IReadOnlyList<PageSize> PageSizes,
    IReadOnlyList<TemplateFieldSuggestion> Fields,
    IReadOnlyList<TemplateStepSuggestion> Steps);