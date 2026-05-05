namespace DWIMS.Service.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";
    public string Provider { get; init; } = "S3";
    public required string BucketName { get; init; }
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string ServiceUrl { get; init; } = string.Empty;
    public string ConnectionString { get; init; } = string.Empty;
    
    public string TemplatePrefix { get; init; } = "templates";
    public string OutputPrefix { get; init; } = "outputs";
    
    public int ExpiryMinutes { get; init; } = 30;
}