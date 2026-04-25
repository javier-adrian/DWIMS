namespace DWIMS.Service.Auth;

public sealed class GoogleOptions
{
    public const string SectionName = "Google";
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public string? HostedDomain { get; init; }
}