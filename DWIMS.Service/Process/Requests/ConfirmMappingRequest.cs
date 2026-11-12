namespace DWIMS.Service.Process.Dtos;

public record ConfirmMappingRequest(
    IReadOnlyList<ConfirmFieldItem> Fields,
    IReadOnlyList<ConfirmStepItem> Steps);