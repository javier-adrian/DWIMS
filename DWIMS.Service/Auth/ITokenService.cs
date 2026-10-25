using DWIMS.Data;

namespace DWIMS.Service.Auth;

public interface ITokenService
{
    string GenerateAccessToken(Data.User user,
        IEnumerable<Role> roles);
    
    Task<string> GenerateRefreshTokenAsync(
        Guid userId,
        CancellationToken cancellationToken = default,
        Guid? rotatedTokenId = null,
        bool revokeAllExisting = true);
    
    Task<RefreshToken?> ValidateRefreshTokenAsync(
        Guid userId,
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task RevokeRefreshTokenAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    string HashToken(string input);
}