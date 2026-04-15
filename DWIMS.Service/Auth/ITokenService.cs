using DWIMS.Data;

namespace DWIMS.Service.Auth;

public interface ITokenService
{
    string GenerateAccessToken(
        User user,
        IEnumerable<Role> roles);
    
    Task<string> GenerateRefreshTokenAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    
    Task<RefreshToken?> ValidateRefreshTokenAsync(
        Guid userId,
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task RevokeRefreshTokenAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}