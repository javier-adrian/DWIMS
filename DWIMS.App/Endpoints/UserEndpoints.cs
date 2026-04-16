namespace DWIMS.Controllers;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("users")
            .WithTags("Users")
            .RequireAuthorization();
        
        group.MapGet("/me", GetCurrentuser)
            .WithDisplayName("Get Current User")
            .WithSummary("Get the current user's profile");
        
        group.MapPut("/me", UpdateCurrentuser)
            .WithDisplayName("Update Current User")
            .WithSummary("Update the current user's profile");
        
        group.MapPut("/me/signature", UploadSignature)
            .WithDisplayName("Upload Signature")
            .WithSummary("Upload a signature for the current user");
        
        return app;
    }

    private static Task UploadSignature(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static Task UpdateCurrentuser(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static Task GetCurrentuser(HttpContext context)
    {
        throw new NotImplementedException();
    }
}