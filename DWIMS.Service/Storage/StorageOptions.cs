namespace DWIMS.Service.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";
    public required string BucketName { get; init; }
    public required string AccessKey { get; init; }
    public required string SecretKey { get; init; }
    public required string ServiceUrl { get; init; }
    public required string ConnectionString { get; init; } = string.Empty;
    
    public string TemplatePrefix { get; init; } = "templates";
    public string OutputPrefix { get; init; } = "outputs";
    
    public int ExpiryMinutes { get; init; } = 30;
}