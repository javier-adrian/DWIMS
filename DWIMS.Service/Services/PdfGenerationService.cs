using DWIMS.Data;
using DWIMS.Service.Common;
using DWIMS.Service.Storage;
using DWIMS.Service.User;
using iText.Forms;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Barcodes;
using System.Globalization;
using DWIMS.Service.Logs;
using iText.Forms.Fields;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SkiaSharp;
using Svg.Skia;

namespace DWIMS.Service.Services;

public sealed class PdfGenerationService(
    IConfiguration configuration,
    ICurrentUserService currentUser,
    AppDbContext context,
    ILogService logService,
    IStorageService storageService) : IPdfGenerationService
{
    private readonly string _appBaseUrl = configuration["App:BaseUrl"] ?? "http://localhost:5244";
    
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
            .Include(s => s.Responses)
                .ThenInclude(r => r.Reviewer)
            .FirstOrDefaultAsync(s => s.Id == submissionId, cancellationToken)
            ?? throw new KeyNotFoundException("Submission not found.");

        var document = submission?.Process?.Documents.FirstOrDefault()
            ?? throw new KeyNotFoundException("Document not found for this submission.");
        
        if (currentUser.UserId != submission.SubmitterId && 
            !currentUser.isSuperAdministrator &&
            !submission.Responses.Any(r => r.ReviewerId == currentUser.UserId || r.AssigneeId == currentUser.UserId))
            throw new UnauthorizedAccessException("You are not authorized to access this submission.");
        
        if (!string.IsNullOrEmpty(submission.ResultLink))
            try
            {
                return await storageService.DownloadAsync(submission.ResultLink, cancellationToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logService.LogAsync(
                    "Result Snapshot Download Failed", 
                    "Submission", 
                    submissionId, 
                    e.ToString(), 
                    cancellationToken);
            }

        var template = await storageService.DownloadAsync(document.Link, cancellationToken);

        var fields = submission.Inputs
            .Where(input => input.Value is not null && !input.Field.IsDeleted)
            .Select(input => (input.Field, Value: input.Value!))
            .ToList();
        
        var signatures = await BuildSignaturesAsync(submission.Responses, cancellationToken);
        
        var verificationLink = $"{_appBaseUrl.TrimEnd('/')}/#/public/status/{submission.Id}";
        
        return RenderPdf(template, fields, signatures, verificationLink, submission.Id);

        // return await storageService.DownloadAsync(document.Link, cancellationToken);
    }

    private async Task<Dictionary<string, SignatureStamp>> BuildSignaturesAsync(
        IEnumerable<Response> responses,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, SignatureStamp>();

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
            
            if (signature is null)
                continue;
            
            result[key] = new SignatureStamp(
                signature.EncryptedBlob,
                $"{response.Reviewer?.FirstName} {response.Reviewer?.LastName}".Trim(),
                response.CompletedOn);
        }
        
        return result;
    }

    private static MemoryStream RenderPdf(
        Stream template,
        IReadOnlyList<(Field Field, string Value)> fields,
        Dictionary<string, SignatureStamp> signatures,
        string verificationLink,
        Guid submissionId)
    {
        var output = new MemoryStream();

        using var reader = new PdfReader(template);
        using var writer = new PdfWriter(
            output, 
            new WriterProperties().SetPdfVersion(PdfVersion.PDF_1_7));
        
        writer.SetCloseStream(false);

        using var result = new PdfDocument(reader, writer);
        
        StampVerification(result, verificationLink, submissionId);
        
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
    
    private static void StampVerification(PdfDocument document, string link, Guid submissionId)
    {
        var page = document.GetFirstPage();
        var pageSize = page.GetPageSize();

        const float qrSize = 64f;
        const float margin = 24f;
        const float gap = 4f;
        const float idFontSize = 6f;
        const float qrPad = 6f;
        const float textPad = 3f;

        // TOP RIGHT
        // var x = pageSize.GetWidth() - margin - qrSize;
        // var y = pageSize.GetHeight() - margin - qrSize;
        
        // BOTTOM LEFT
        var x = margin;

        var captionBoxY = margin - textPad;
        var captionBoxHeight = idFontSize + 2 * textPad;
        var qrBoxY = captionBoxY + captionBoxHeight + gap;
        var qrY = qrBoxY + qrPad;

        var canvas = new PdfCanvas(page);
        
        var idText = submissionId.ToString().ToUpperInvariant();
        var font = PdfFontFactory.CreateFont(StandardFonts.COURIER_BOLD);
        var idWidth = font.GetWidth(idText, idFontSize);
        var qrBoxWidth = qrSize + 2;
        var textBoxWidth = idWidth + 2 * textPad;
        
        
        // TOP RIGHT
        // var textX = x + qrSize - font.GetWidth(idText, idFontSize);
        // var textY = y - gap - idFontSize;
        
        // BOTTOM LEFT
        // var textX = x;
        // var textY = y + qrSize + gap;

        canvas.SaveState()
            .SetFillColor(ColorConstants.WHITE)
            .Rectangle(
                x - qrPad,
                qrBoxY,
                qrBoxWidth,
                qrSize + 2 * qrPad)
            .Fill()
            .RestoreState();
        
        var form = new BarcodeQRCode(link).CreateFormXObject(document);
        var bbox = form.GetBBox().ToRectangle();
        canvas.AddXObjectWithTransformationMatrix(
            form, 
            qrSize / bbox.GetWidth(), 0, 
            0, qrSize / bbox.GetHeight(), 
            x, qrY);
        
        canvas.SaveState()
            .SetFillColor(ColorConstants.WHITE)
            .Rectangle(
                x - qrPad,
                captionBoxY,
                textBoxWidth,
                captionBoxHeight)
            .Fill()
            .RestoreState();
        
        canvas.SaveState()
            .BeginText()
            .SetFillColor(ColorConstants.BLACK)
            .SetFontAndSize(font, idFontSize)
            .SetTextMatrix(x, captionBoxY + textPad + 1f)
            .ShowText(idText)
            .EndText()
            .RestoreState();
        
        canvas.Release();
    }

    private static void FillFields(
        PdfAcroForm form,
        IReadOnlyList<(Field Field, string Value)> fields)
    {
        foreach (var (key, value) in fields)
        {
            var field = form.GetField(key.AcroFormKey);

            if (field is null)
                continue;

            foreach (var widget in field.GetWidgets())
            {
                var mk = widget.GetPdfObject().GetAsDictionary(PdfName.MK);
                if (mk is not null)
                    mk.Remove(PdfName.BG);
            }

            if (key.Type == InputTypes.Checkbox)
            {
                if (value.Length > 0)
                    field.SetValue(value);
            }
            else
            {
                field.SetJustification(TextAlignment.CENTER);
                field.SetValue(value);
            }
            
            field.SetReadOnly(true);
        }
    }

    private static void FillSignatures(
        PdfAcroForm form,
        PdfDocument document,
        Dictionary<string, SignatureStamp> signatures)
    {
        foreach (var (key, stamp) in signatures)
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
            var pngBytes = ConvertSvgToPng(stamp.Svg);
            var imageData = ImageDataFactory.Create(pngBytes);
            var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            var bandHeight = stamp.CompletedOn is null ? 12f : 20f;
            
            canvas
                .SaveState()
                .AddImageFittedIntoRectangle(imageData, rectangle, true);
            
            canvas.RestoreState();
            
            var nameRect = new Rectangle(
                rectangle.GetX(),
                rectangle.GetY(),
                rectangle.GetWidth(),
                bandHeight);
            
            var nameCanvas = new iText.Layout.Canvas(canvas, nameRect);
            
            nameCanvas.Add(new Paragraph(stamp.Name)
                .SetFont(font)
                .SetFontSize(8)
                .SetMargin(0)
                .SetMultipliedLeading(1)
                .SetTextAlignment(TextAlignment.CENTER));

            if (stamp.CompletedOn is { } completedOn)
                nameCanvas.Add(new Paragraph(completedOn.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture))
                    .SetFont(font)
                    .SetFontSize(7)
                    .SetMargin(0)
                    .SetMultipliedLeading(1)
                    .SetTextAlignment(TextAlignment.CENTER));
            
            nameCanvas.Close();
            
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