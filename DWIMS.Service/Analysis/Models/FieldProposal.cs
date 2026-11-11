using DWIMS.Data;

namespace DWIMS.Service.Analysis;

public sealed record FieldProposal(
    string Title,
    InputTypes InputType,
    double X,
    double Y,
    double Width,
    double Height,
    double Confidence,
    string Evidence);