namespace DWIMS.Service.Analysis;

public sealed record StepProposal(
    string Title,
    int Order,
    double X,
    double Y,
    double Width,
    double Height);