using DWIMS.Service.Common;
using iText.Forms.Fields;
using iText.Kernel.Pdf;

namespace DWIMS.Service.Services;

public sealed class AcroFormService : IAcroFormService
{
    public Task<Result<IReadOnlyList<AcroFormField>>> ExtractFieldsAsync(Stream pdfStream, CancellationToken cancellationToken = default)
    {
        try
        {
            var reader = new PdfReader(pdfStream);
            using var pdfDoc = new PdfDocument(reader);

            var acroForm = iText.Forms.PdfAcroForm.GetAcroForm(pdfDoc, false);

            if (acroForm is null)
                return Task.FromResult(Result<IReadOnlyList<AcroFormField>>.Failure(
                    "NOT_AN_ACROFORM", "The uploaded PDF does not contain an AcroForm."));

            var allFields = acroForm.GetAllFormFields();

            if (allFields.Count == 0)
                return Task.FromResult(Result<IReadOnlyList<AcroFormField>>.Failure(
                    "NOT_AN_ACROFORM", "The uploaded PDF does not contain any form fields."));

            var extractedFields = new List<AcroFormField>();

            foreach (var (key, formField) in allFields)
            {
                var fieldName = formField.GetFieldName()?.ToUnicodeString() ?? key;
                var formType = formField.GetFormType();
                var isSignature = formType != null && formField is PdfSignatureFormField;

                var title = fieldName;
                if (fieldName.Contains(':', StringComparison.Ordinal))
                    title = fieldName[(fieldName.IndexOf(':') + 1)..].Trim();

                extractedFields.Add(new AcroFormField(fieldName, title, isSignature));
            }

            return Task.FromResult(Result<IReadOnlyList<AcroFormField>>.Success(extractedFields));
        }
        catch (Exception)
        {
            return Task.FromResult(Result<IReadOnlyList<AcroFormField>>.Failure(
                "PDF_READ_ERROR", "Failed to read the PDF file."));
        }
    }
}
