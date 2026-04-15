using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DWIMS.Data;
using DWIMS.Service.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace DWIMS.Service.Services;

public class TokenService(
    AppDbContext context,
    IOptions<JwtOptions> jwtOptions
    ) : ITokenService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;
    
    public string GenerateAccessToken(User user, IEnumerable<Role> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
            claims.Add(new Claim(
                DwimsClaims.Role,
                $"{role.Department.Id}:{role.Title}"
            ));

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        
        var credentials = new SigningCredentials(
            key, 
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpiry),
            signingCredentials: credentials);
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string> GenerateRefreshTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await context.RefreshTokens
            .Where(x => x.UserId == userId && !x.Revoked)
            .ToListAsync(cancellationToken);
        
        foreach (var token in existing)
            token.Revoked = true;
        
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = BCrypt.Net.BCrypt.HashPassword(rawToken),
            UserId = userId,
            Created = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(7),
        });

        await context.SaveChangesAsync(cancellationToken);
        
        return rawToken;
    }

    public Task<string> ValidateRefreshTokenAsync(Guid userId, string refreshToken, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task RevokeRefreshTokenAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}