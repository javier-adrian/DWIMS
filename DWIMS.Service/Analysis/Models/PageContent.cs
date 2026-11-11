namespace DWIMS.Service.Analysis;

public sealed record PageContent(
    int Index,
    double Width,
    double Height,
    IReadOnlyList<WordBox> Words,
    IReadOnlyList<RuleSegment> Segments);