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
            .Produces(StatusCodes.Status201Created)
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
            .WithDisplayName("Forgot Password")
            .WithSummary("Reset a user's password");
        
        group.MapPost("/reset-password", ResetPassword)
            .ProducesValidationProblem()
            .WithDisplayName("Reset Password")
            .WithSummary("Reset a user's password");
        
        return app;
    }

    private static async Task<IResult> Register(RegisterRequest request, IAuthService authService, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);
        return result.ToCreatedResult("/users/me");
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
    private static Task<IResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken) => throw new NotImplementedException();
    private static Task<IResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken) => throw new NotImplementedException();
}