namespace DWIMS.Service.Common;

public sealed class EmailOptions
{
    public const string SectionName = "Email";
    public required string Host { get; init; }
    public required int Port { get; init; }
    public bool UseSsl { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
    public required string From { get; init; }
    public required string FromName { get; init; }
}