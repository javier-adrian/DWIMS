using DWIMS.Data;

namespace DWIMS.Service.Process.Dtos;

public sealed record FieldDto(
    Guid Id,
    string Name,
    string? AcroFormKey,
    InputTypes Type,
    bool Required,
    string? Options,
    int Order,
    string? Pattern,
    string? PatternMessage,
    int? RectPage = null,
    double? RectX = null,
    double? RectY = null,
    double? RectWidth = null,
    double? RectHeight = null);