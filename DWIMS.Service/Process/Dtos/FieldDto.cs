using DWIMS.Data;

namespace DWIMS.Service.Process.Dtos;

public sealed record FieldDto(
    Guid Id,
    string Name,
    string AcroFormKey,
    InputTypes Type,
    bool Required);