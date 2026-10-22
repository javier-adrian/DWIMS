namespace DWIMS.Service.Storage;

public static class PrefixExtensions
{
    public static string Resolve(
        this Prefix prefix,
        StorageOptions options) => prefix switch
    {
        Prefix.Template => options.TemplatePrefix,
        Prefix.Output => options.OutputPrefix,
        Prefix.Attachment => options.AttachmentPrefix,
        Prefix.Document => options.DocumentPrefix,
        _ => throw new ArgumentOutOfRangeException(
            nameof(prefix), 
            prefix, 
            "Unknown prefix"),
    };
}