using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

namespace DWIMS.Service.Analysis;

public class PdfTextLayerExtractor
{
    public PageContent Extract(PdfPage page, int index)
    {
        var size = page.GetPageSize();

        var listener = new PageContentListener(size.GetWidth(), size.GetHeight());
        
        new PdfCanvasProcessor(listener).ProcessPageContent(page);
        
        listener.Complete();

        return new PageContent(
            index,
            size.GetWidth(),
            size.GetHeight(),
            listener.Words,
            listener.Segments);
    }
}