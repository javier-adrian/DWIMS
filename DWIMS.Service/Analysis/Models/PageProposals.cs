namespace DWIMS.Service.Analysis;

public sealed record PageProposals(
    IReadOnlyList<FieldProposal> Fields,
    IReadOnlyList<StepProposal> Steps);