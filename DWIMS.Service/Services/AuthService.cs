using System.Security.Cryptography;
using DWIMS.Data;
using DWIMS.Service.Auth;
using DWIMS.Service.Auth.Dtos;
using DWIMS.Service.Auth.Requests;
using DWIMS.Service.Common;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DWIMS.Service.Services;

public class AuthService(
    AppDbContext context,
    ITokenService tokenService,
    INotificationService notificationService,
    IOptions<GoogleOptions> googleOptions
) : IAuthService
{
    private readonly GoogleOptions _googleOptions = googleOptions.Value;
    
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

    public async Task<Result<AuthResponse>> RefreshTokenAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var hashedToken = tokenService.HashToken(request.RefreshToken);

        var refreshToken = await context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == hashedToken && !rt.Revoked && rt.Expires > DateTime.UtcNow,
                cancellationToken);

        if (refreshToken is null)
            return Result<AuthResponse>.Failure("INVALID_REFRESH_TOKEN", "Refresh token is invalid or expired.");

        return Result<AuthResponse>.Success(await BuildAuthResponseAsync(refreshToken.User, cancellationToken));
    }

    public async Task ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email;
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null || user.Password is null) return;
        
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        
        context.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            Token = token,
            UserId = user.Id,
            Created = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddHours(1),
        });

        await context.SaveChangesAsync(cancellationToken);
        try
        { 
            await notificationService.SendPasswordResetAsync(user.Id, token, cancellationToken);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<Result> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var resetToken = await context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == request.Token && !t.Used && t.Expires > DateTime.UtcNow, cancellationToken);

        if (resetToken is null)
            return Result.Failure("INVALID_TOKEN", "Reset token is invalid or expired.");

        resetToken.User.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        resetToken.Used = true;
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<AuthResponse>> GoogleOAuthAsync(GoogleOAuthRequest request, CancellationToken cancellationToken = default)
    {
        GoogleJsonWebSignature.Payload payload;

        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [_googleOptions.ClientId],
                HostedDomain = _googleOptions.HostedDomain
            };
            
            payload = await GoogleJsonWebSignature
                .ValidateAsync(request.Token);
        }
        catch (InvalidJwtException e)
        {
            return Result<AuthResponse>.Failure(
                "INVALID_GOOGLE_TOKEN",
                "The Google ID token is invalid or expired.");
        }

        if (_googleOptions.HostedDomain is not null &&
            payload.Email.EndsWith($"{_googleOptions.HostedDomain}",
                StringComparison.OrdinalIgnoreCase))
        {
            return Result<AuthResponse>.Failure(
                "UNAUTHORIZED_DOMAIN",
                $"Only users from @{_googleOptions.HostedDomain} are allowed to login.");
        }

        var externalLogin = await context.ExternalLogins
            .Include(login => login.User)
            .FirstOrDefaultAsync(login =>
                    login.Provider == ExternalLoginProvider.Google &&
                    login.ProviderSubject == payload.Subject,
                cancellationToken);

        Data.User user;

        if (externalLogin is not null)
        {
            user = externalLogin.User;
        }
        else
        {
            user = await context.Users
                       .FirstOrDefaultAsync(user =>
                               user.Email == payload.Email,
                           cancellationToken)
                   ?? await CreateOAuthUserAsync(
                       payload,
                       payload.Email,
                       cancellationToken);

            context.ExternalLogins.Add(new ExternalLogin
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = ExternalLoginProvider.Google,
                ProviderSubject = payload.Subject,
                Created = DateTime.UtcNow
            });

            await context.SaveChangesAsync(cancellationToken);
        }

        if (user.IsDeleted)
            return Result<AuthResponse>.Failure(
                "ACCOUNT_DELETEd",
                "The account has been deleted.");

        return Result<AuthResponse>.Success(
            await BuildAuthResponseAsync(user, cancellationToken));
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

    private async Task<Data.User> CreateOAuthUserAsync(
        GoogleJsonWebSignature.Payload payload,
        string Email,
        CancellationToken cancellationToken = default)
    {
        var user = new Data.User
        {
            Id = Guid.NewGuid(),
            FirstName = payload.GivenName,
            LastName = payload.FamilyName,
            Email = Email
        };
        
        context.Users.Add(user);

        return user;
    }
}