namespace DWIMS.Service.Analysis;

public sealed record RuleSegment(
    double Start,
    double End,
    double Position,
    bool Horizontal);