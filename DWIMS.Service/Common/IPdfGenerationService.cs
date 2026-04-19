namespace DWIMS.Service.Common;

public interface IPdfGenerationService
{
    Task<Stream> GenerateAsync(Guid submissionId, CancellationToken cancellationToken = default);
}