namespace DWIMS.Service.Process.Dtos;

public sealed record SlotEntryDto(
    Guid stepId,
    int Page,
    double SignatureX,
    double SignatureY,
    double SignatureWidth,
    double SignatureHeight
    );