using DWIMS.Data;

namespace DWIMS.Service.Common;

public record AcroFormField(
    string Name,
    string Title,
    bool IsSignature,
    string? Options,
    InputTypes Type = InputTypes.Text);

public interface IAcroFormService
{
    Task<Result<IReadOnlyList<AcroFormField>>> ExtractFieldsAsync(Stream pdfStream, CancellationToken cancellationToken = default);
}
