namespace DWIMS.Service.Auth.Dtos;

public record AuthResponse(
    string Token,
    string RefreshToken);