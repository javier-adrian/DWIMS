namespace DWIMS.Service.Analysis;

public sealed record TemplateStepSuggestion(
    string Title,
    int Order,
    int Page,
    double X,
    double Y,
    double Width,
    double Height);