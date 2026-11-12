namespace DWIMS.Service.Process.Dtos;

public record ConfirmFieldItem(
    string Title,
    int Type,
    bool Required,
    int Page,
    double X,
    double Y,
    double Width,
    double Height,
    string? Pattern = null,
    string? PatternMessage = null);