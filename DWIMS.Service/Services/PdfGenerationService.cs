using DWIMS.Data;
using DWIMS.Service.Common;
using DWIMS.Service.Storage;
using iText.Forms;
using iText.IO.Image;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout.Element;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using Svg.Skia;

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
            .Include(s => s.Process)
                .ThenInclude(p => p.Fields)
            .Include(s => s.Inputs)
                .ThenInclude(i => i.Field)
            .Include(s => s.Responses)
                .ThenInclude(r => r.Step)
            .FirstOrDefaultAsync(s => s.Id == submissionId, cancellationToken)
            ?? throw new KeyNotFoundException("Submission not found.");

        var document = submission?.Process?.Documents.FirstOrDefault()
            ?? throw new KeyNotFoundException("Document not found for this submission.");

        var template = await storageService.DownloadAsync(document.Link, cancellationToken);
        
        var fields = submission.Inputs
            .Where(input => input.Value is not null && !input.Field.IsDeleted)
            .ToDictionary(input => input.Field.AcroFormKey, input => input.Value!);
        
        var signatures = await BuildSignaturesAsync(submission.Responses, cancellationToken);
        
        return RenderPdf(template, fields, signatures);

        // return await storageService.DownloadAsync(document.Link, cancellationToken);
    }

    private async Task<Dictionary<string, byte[]>> BuildSignaturesAsync(
        IEnumerable<Response> responses,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, byte[]>();

        var approved = responses
            .Where(response =>
                response.Result == Status.Approve &&
                response.ReviewerId.HasValue &&
                !response.Step.IsDeleted)
            .ToList();

        foreach (var response in approved)
        {
            var key = $"{response.Step.Order.ToString()}: {response.Step.Title}";
            
            var signature = await context.Signatures
                .IgnoreQueryFilters()
                .Where(signature => 
                    signature.UserId == response.ReviewerId!.Value)
                .OrderByDescending(signature => signature.Created)
                .FirstOrDefaultAsync(cancellationToken);
            
            result[key] = signature.EncryptedBlob;
        }
        
        return result;
    }

    private static MemoryStream RenderPdf(
        Stream template,
        Dictionary<string, string> fields,
        Dictionary<string, byte[]> signatures)
    {
        var output = new MemoryStream();

        using var reader = new PdfReader(template);
        using var writer = new PdfWriter(
            output, 
            new WriterProperties().SetPdfVersion(PdfVersion.PDF_1_7));
        
        writer.SetCloseStream(false);

        using var result = new PdfDocument(reader, writer);
        var document = PdfAcroForm.GetAcroForm(result, createIfNotExist: false);

        if (document is null)
        {
            result.Close();
            output.Position = 0;
            return output;
        }
        
        FillFields(document, fields);
        FillSignatures(document, result, signatures);
        
        document.FlattenFields();
        
        result.Close();
        output.Position = 0;
        
        return output;
    }

    private static void FillFields(
        PdfAcroForm form,
        Dictionary<string, string> fields)
    {
        foreach (var (key, value) in fields)
        {
            var field = form.GetField(key);

            if (field is null)
                continue;

            foreach (var widget in field.GetWidgets())
            {
                var mk = widget.GetPdfObject().GetAsDictionary(PdfName.MK);
                if (mk is not null)
                    mk.Remove(PdfName.BG);
            }

            field.SetValue(value);
            field.SetReadOnly(true);
        }
    }

    private static void FillSignatures(
        PdfAcroForm form,
        PdfDocument document,
        Dictionary<string, byte[]> signatures)
    {
        foreach (var (key, signature) in signatures)
        {
            var field = form.GetField(key);
            
            if (field is null)
                continue;
            
            var widget = field.GetWidgets().FirstOrDefault();
            if (widget is null)
                continue;

            var rectangle = widget.GetRectangle().ToRectangle();
            var pageNumber = document.GetPageNumber(widget.GetPage());
            var page = document.GetPage(pageNumber);
            var canvas = new PdfCanvas(page);
            var pngBytes = ConvertSvgToPng(signature);
            var imageData = ImageDataFactory.Create(pngBytes);

            canvas
                .SaveState()
                .AddImageFittedIntoRectangle(imageData, rectangle, true);
            
            canvas.RestoreState();
            canvas.Release();
            
            field.SetReadOnly(true);
        }
    }

    private static byte[] ConvertSvgToPng(byte[] svgBytes)
    {
        using var svgStream = new MemoryStream(svgBytes);
        var svg = new SKSvg();
        svg.Load(svgStream);
        var picture = svg.Picture;
        var width = (int)Math.Ceiling(picture.CullRect.Width);
        var height = (int)Math.Ceiling(picture.CullRect.Height);

        if (width <= 0 || height <= 0)
            throw new InvalidOperationException("SVG has invalid dimensions.");

        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.DrawPicture(picture);
        canvas.Flush();

        using var stream = new MemoryStream();
        bitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
        return stream.ToArray();
    }
}