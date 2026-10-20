namespace DWIMS.Service.Auth.Requests;

public sealed record VerifyEmailRequest(
    string Code,
    string Email);