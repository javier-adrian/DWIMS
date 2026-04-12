namespace DWIMS.Service.Auth.Requests;

public sealed class ForgotPasswordRequest
{
    public required string Email { get; set; }
}