namespace DWIMS.Service.Auth.Requests;

public sealed class ResetPasswordRequest
{
    public required Guid UserId { get; set; }
    public required string Token { get; set; }
    public required string NewPassword { get; set; }
}