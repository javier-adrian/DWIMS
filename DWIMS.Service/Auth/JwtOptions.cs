namespace DWIMS.Service.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    
    public required string Secret { get; init; }
    
    public required string Issuer { get; init; }
    
    public required string Audience { get; init; }

    public int AccessTokenExpiry { get; init; } = 15;
    public int RefreshTokenExpiry { get; init; } = 7 * 24 * 60 * 60;
}