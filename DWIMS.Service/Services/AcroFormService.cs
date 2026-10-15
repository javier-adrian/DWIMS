using System.Text.Json;
using DWIMS.Data;
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
                
                var type = InputTypes.Text;
                string? options = null;
                
                if (formField is PdfButtonFormField button && !button.IsRadio() && !button.IsPushButton())
                {
                    type = InputTypes.Checkbox;
                    var onState = GetOnState(formField);
                    
                    if (onState is not null)
                        options = JsonSerializer.Serialize(new { on = onState });
                } else if (formField is PdfChoiceFormField choice && choice.IsCombo())
                {
                    type = InputTypes.Dropdown;
                    options = GetOptions(choice);
                }

                var title = fieldName;
                if (fieldName.Contains(':', StringComparison.Ordinal))
                    title = fieldName[(fieldName.IndexOf(':') + 1)..].Trim();

                // the "N-" prefix is the order marker ProcessService parses into Field.Order;
                // only the label after it is user-facing
                if (title.Contains('-') && int.TryParse(title.Split('-')[0].Trim(), out _))
                    title = title[(title.IndexOf('-') + 1)..].Trim();

                extractedFields.Add(new AcroFormField(fieldName, title, isSignature, options, type));
            }

            return Task.FromResult(Result<IReadOnlyList<AcroFormField>>.Success(extractedFields));
        }
        catch (Exception)
        {
            return Task.FromResult(Result<IReadOnlyList<AcroFormField>>.Failure(
                "PDF_READ_ERROR", "Failed to read the PDF file."));
        }
    }

    private static string? GetOnState(PdfFormField field)
    {
        var states = field
            .GetWidgets()
            .FirstOrDefault()?
            .GetPdfObject()
            .GetAsDictionary(PdfName.AP)?
            .GetAsDictionary(PdfName.D);
        return states?.KeySet().OfType<PdfName>()
            .FirstOrDefault(name => name.GetValue() != "Off")?
            .GetValue();
    }

    private static string? GetOptions(PdfChoiceFormField field)
    {
        var options = field.GetPdfObject().GetAsArray(PdfName.Opt);
        if (options is null) return null;
        
        var list = new List<object>();
        
        for (var i = 0; i < options.Size(); i++)
            if (options.Get(i) is PdfString str)
                list.Add(new { value = str.ToUnicodeString(), label = str.ToUnicodeString() });
            else if (options.Get(i) is PdfArray pair && 
                     pair.Get(0) is PdfString v && 
                     pair.Get(1) is PdfString l)
                list.Add(new { value = v.ToUnicodeString(), label = l.ToUnicodeString() });
        
        return list.Count == 0 ? null : JsonSerializer.Serialize(list);
    }
}
