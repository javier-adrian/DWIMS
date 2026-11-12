using DWIMS.Service.Common;
using iText.Kernel.Pdf;

namespace DWIMS.Service.Analysis;

public sealed class TemplateAnalysisEngine(
    PdfTextLayerExtractor extractor,
    IFormHeuristicsEngine engine) : ITemplateAnalysisEngine
{
    public Task<Result<TemplateAnalysis>> AnalyzeAsync(Stream pdf, CancellationToken cancellationToken = default)
    {
        try
        {
            if (pdf.CanSeek) pdf.Position = 0;

            using var reader = new PdfReader(pdf);
            reader.SetCloseStream(false);
            using var doc = new PdfDocument(reader);

            var pageCount = doc.GetNumberOfPages();
            var sizes = new List<PageSize>(pageCount);
            var fields = new List<TemplateFieldSuggestion>();
            var steps = new List<TemplateStepSuggestion>();
            var stepOrder = 0;

            for (var i = 1; i <= pageCount; i++)
            {
                var content = extractor.Extract(doc.GetPage(i), i - 1);
                var proposals = engine.Propose(content);
                
                sizes.Add(new PageSize(content.Width, content.Height));

                foreach (var field in proposals.Fields)             
                    fields.Add(new TemplateFieldSuggestion(
                        field.Title,
                        (int) field.InputType,
                        content.Index,
                        Math.Round(field.X / content.Width, 4),
                        Math.Round(field.Y / content.Height, 4),
                        Math.Round(field.Width / content.Width, 4),
                        Math.Round(field.Height / content.Height, 4),
                        Math.Round(field.Confidence, 2),
                        field.Evidence));

                foreach (var step in proposals.Steps)
                    steps.Add(new TemplateStepSuggestion(
                        step.Title,
                        ++stepOrder,
                        content.Index,
                        Math.Round(step.X / content.Width, 4),
                        Math.Round(step.Y / content.Height, 4),
                        Math.Round(step.Width / content.Width, 4),
                        Math.Round(step.Height / content.Height, 4)));
            }

            return Task.FromResult(Result<TemplateAnalysis>.Success(
                new TemplateAnalysis(pageCount, sizes, fields, steps)));
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            return Task.FromResult(Result<TemplateAnalysis>.Failure(
                "ANALYSIS_FAILED", "Failed to analyze the template document."));
        }
    }
}