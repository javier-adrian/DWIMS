using DWIMS.Data;

namespace DWIMS.Service.Routing;

public record RouteResult(
    RouteOutcome outcome,
    Step? nextStep,
    IReadOnlyList<string>? skippedSteps = null);