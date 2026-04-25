namespace DWIMS.Service.Auth.Requests;

public sealed class GoogleOAuthRequest
{
    public required Guid Token { get; set; }
}