using DWIMS.Service.Auth;
using DWIMS.Service.Auth.Requests;
using DWIMS.Service.Services;

namespace DWIMS.Controllers;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("auth")
            .WithTags("Auth")
            .AllowAnonymous();
        
        group.MapPost("/register", Register)
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .WithDisplayName("Register")
            .WithSummary("Register a new user");
        
        group.MapPost("/login", Login)
            .ProducesValidationProblem()
            .WithDisplayName("Login")
            .WithSummary("Authenticate with a username and password");
        
        group.MapPost("/refresh", Refresh)
            .ProducesValidationProblem()
            .WithDisplayName("Refresh")
            .WithSummary("Acquire a new access token using a refresh token");

        group.MapPost("/forgot-password", ForgotPassword)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status204NoContent)
            .WithDisplayName("Forgot Password")
            .WithSummary("Reset a user's password");
        
        group.MapPost("/reset-password", ResetPassword)
            .ProducesValidationProblem()
            .WithDisplayName("Reset Password")
            .WithSummary("Reset a user's password");
        
        group.MapPost("/verify-email", VerifyEmail)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status200OK)
            .WithDisplayName("Verify Email")
            .WithSummary("Verify a user's email with a code");
        
        group.MapPost("/resend-verification", ResendVerification)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status200OK)
            .WithDisplayName("Resend Verification Code")
            .WithSummary("Resend a verification code to a user's email");
        
        group.MapPost("oauth/google", GoogleOAuth)
            .WithDisplayName("Google OAuth")
            .WithSummary("Authenticate with Google");
        
        return app;
    }

    private static async Task<IResult> ResendVerification(
        ResendVerificationRequest request,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.ResendVerificationAsync(request, cancellationToken);
        
        return result.ToOkResult();
    }

    private static async Task<IResult> VerifyEmail(
        VerifyEmailRequest request, 
        IAuthService authService, 
        CancellationToken cancellationToken)
    {
        var result = await authService.VerifyEmailAsync(request, cancellationToken);
        
        return result.ToOkResult();
    }

    private static async Task<IResult> GoogleOAuth(
        GoogleOAuthRequest request,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.GoogleOAuthAsync(request, cancellationToken);
        return result.ToOkResult();
    }

    private static async Task<IResult> Register(RegisterRequest request, IAuthService authService, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);
        return result.ToOkResult();
    }

    private static async Task<IResult> Login(LoginRequest request, IAuthService authService, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        return result.ToOkResult();
    }
    private static async Task<IResult> Refresh(RefreshTokenRequest request, IAuthService authService, CancellationToken cancellationToken)
    {
        var result = await authService.RefreshTokenAsync(request, cancellationToken);
        return result.ToOkResult();
    }

    private static async Task<IResult> ForgotPassword(ForgotPasswordRequest request, IAuthService authService, CancellationToken cancellationToken)
    {
        await authService.ForgotPasswordAsync(request, cancellationToken);
        return Results.NoContent();
    }
    private static async Task<IResult> ResetPassword(ResetPasswordRequest request, IAuthService authService, CancellationToken cancellationToken)
    {
        var result = await authService.ResetPasswordAsync(request, cancellationToken);
        return result.IsSuccess
            ? Results.Ok()
            : Results.UnprocessableEntity(new { result.Error, result.ErrorDescription });
    }
}