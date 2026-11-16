using DWIMS.Data;

namespace DWIMS.Service.Process.Requests;

public sealed record SetStepConditionRequest(
    Guid? ConditionFieldId = null,
    ConditionOperator? ConditionOperator= null,
    string? ConditionValue = null);