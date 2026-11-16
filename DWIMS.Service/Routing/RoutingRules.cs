using System.Globalization;
using DWIMS.Data;

namespace DWIMS.Service.Routing;

public static class RoutingRules
{
    public static bool ShouldInclude(Step step, IReadOnlyDictionary<Guid, string> valuesByFieldId)
    {
        if (step.ConditionFieldId is null)
            return true;

        if (!valuesByFieldId.TryGetValue(step.ConditionFieldId.Value, out var actual))
            return true;

        var expected = (step.ConditionValue ?? "").Trim();
        var value = actual.Trim();

        return step.ConditionOperator switch
        {
            ConditionOperator.Equals => string.Equals(value, expected, StringComparison.Ordinal),
            ConditionOperator.NotEquals => !string.Equals(value, expected, StringComparison.Ordinal),
            ConditionOperator.GreaterThan
                when decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) &&
                     decimal.TryParse(expected, NumberStyles.Number, CultureInfo.InvariantCulture, out var e) =>
                v > e,
            ConditionOperator.LessThan
                when decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) &&
                     decimal.TryParse(expected, NumberStyles.Number, CultureInfo.InvariantCulture, out var e) =>
                v < e,
            _ => true
        };
    }

    public static string Describe(Step step, string? fieldTitle, string? actualValue) =>
        $"{step.Title}: skipped by rule (field '{fieldTitle ?? step.ConditionFieldId.ToString()}' " +
        $"{Op(step.ConditionOperator)} '{step.ConditionValue}', " +
        $"actual '{actualValue ?? ""}'";

    private static string Op(ConditionOperator op) => op switch
    {
        ConditionOperator.Equals => "equals",
        ConditionOperator.NotEquals => "not equals",
        ConditionOperator.GreaterThan => "greater than",
        ConditionOperator.LessThan => "less than",
        _ => "matches"
    };
}