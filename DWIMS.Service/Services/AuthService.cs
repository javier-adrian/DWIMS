using DWIMS.Data;
using DWIMS.Service.Auth;
using DWIMS.Service.Auth.Dtos;
using DWIMS.Service.Auth.Requests;
using DWIMS.Service.Common;
using Microsoft.EntityFrameworkCore;

namespace DWIMS.Service.Services;

public class AuthService(
    AppDbContext context,
    ITokenService tokenService
    ) : IAuthService
{
    public async Task<Result<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        if (await context.Users.AnyAsync(u => u.Email == request.Email, cancellationToken))
            return Result<AuthResponse>.Failure(
                "EMAIL_TAKEN", 
                "An account with this email already exists.");

        var user = new Data.User
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName,
            MiddleName = request.MiddleName ?? "",
            LastName = request.LastName,
            Email = request.Email,
            ContactNumber = request.ContactNumber,
            Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
        };
        
        context.Users.Add(user);

        await context.SaveChangesAsync(cancellationToken);

        return Result<AuthResponse>.Success(await BuildAuthResponseAsync(user, cancellationToken));
    }

    public async Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(
                x => x.Email == request.Email, 
                cancellationToken);
        
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            return Result<AuthResponse>.Failure(
                "INVALID_CREDENTIALS", 
                "Invalid email or password.");
        
        return Result<AuthResponse>.Success(await BuildAuthResponseAsync(user, cancellationToken));
    }

    public Task<Result<AuthResponse>> RefreshTokenAsync(
        RefreshTokenRequest request, 
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(
        Data.User user,
        CancellationToken cancellationToken = default
        )
    {
        var roles = await context.Roles
            .Include(role => role.Department)
            .Where(role => role.UserId == user.Id)
            .ToListAsync(cancellationToken);

        var accessToken = tokenService.GenerateAccessToken(user, roles);
        var refreshToken = await tokenService.GenerateRefreshTokenAsync(user.Id, cancellationToken);
        
        return new AuthResponse(accessToken, refreshToken);
    } 
}