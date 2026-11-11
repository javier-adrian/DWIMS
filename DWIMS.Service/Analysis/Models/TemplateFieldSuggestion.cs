namespace DWIMS.Service.Analysis;

public sealed record TemplateFieldSuggestion(
    string Title,
    int Type,
    int Page,
    double X,
    double Y,
    double Width,
    double Height,
    double Confidence,
    string Evidence);