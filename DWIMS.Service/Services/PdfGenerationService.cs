using DWIMS.Data;
using DWIMS.Service.Common;
using DWIMS.Service.Storage;
using Microsoft.EntityFrameworkCore;

namespace DWIMS.Service.Services;

public sealed class PdfGenerationService(
    AppDbContext context,
    IStorageService storageService) : IPdfGenerationService
{
    public async Task<Stream> GenerateAsync(Guid submissionId, CancellationToken cancellationToken = default)
    {
        var submission = await context.Submissions
            .Include(s => s.Process)
            .ThenInclude(p => p.Documents)
            .FirstOrDefaultAsync(s => s.Id == submissionId, cancellationToken);

        var document = submission?.Process?.Documents.FirstOrDefault()
            ?? throw new KeyNotFoundException("Document not found for this submission.");

        return await storageService.DownloadAsync(document.Link, cancellationToken);
    }
}