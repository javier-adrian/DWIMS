namespace DWIMS.Service.Common;

public class PdfSigningOptions
{
    public const string SectionName = "PdfSigning";
    public string? CertificatePath { get; set; }
    public string? CertificatePassword { get; set; }
    public string? TsaUrl { get; set; }
}