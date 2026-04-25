namespace DWIMS.Service.Auth.Requests;

public sealed class GoogleOAuthRequest
{
    public required string Token { get; set; }
    public required string Guid { get; set; }
}