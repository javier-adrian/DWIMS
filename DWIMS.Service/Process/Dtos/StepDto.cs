using DWIMS.Data;

namespace DWIMS.Service.Process.Dtos;

public sealed record StepDto(
    Guid Id,
    int Order,
    string Name,
    GeneralRole Role,
    int? SignaturePage = null,
    double? SignatureX = null,
    double? SignatureY = null,
    double? SignatureWidth = null,
    double? SignatureHeight = null,
    Guid? ConditionFieldId = null,
    ConditionOperator? ConditionOperator = null,
    string? ConditionValue = null);