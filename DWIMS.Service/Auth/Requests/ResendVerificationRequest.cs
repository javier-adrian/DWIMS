namespace DWIMS.Service.Auth.Requests;

public sealed record ResendVerificationRequest(
    string Email);