namespace DWIMS.Service.CurrentUser.Requests;

public sealed class RegisterSignatureRequest
{
    public string? SvgContent { get; set; }
    public string? ImageBase64 { get; set; }
}