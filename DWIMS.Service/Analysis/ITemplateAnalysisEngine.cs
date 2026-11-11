using DWIMS.Service.Common;

namespace DWIMS.Service.Analysis;

public interface ITemplateAnalysisEngine
{
    Task<Result<TemplateAnalysis>> AnalyzeAsync(Stream pdf, CancellationToken cancellationToken = default);
}