using DWIMS.Data;
using DWIMS.Service.Auth;
using DWIMS.Service.CurrentUser;
using DWIMS.Service.Department;
using DWIMS.Service.Department.Requests;
using DWIMS.Service.Services;

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

        var roleGroup = app.MapGroup("roles")
            .WithTags("Roles")
            .RequireAuthorization(DwimsPolicies.Administrator);

        roleGroup.MapPost("/", AssignRole)
            .WithDisplayName("Assign Role")
            .WithSummary("Assign a role to a user");

        roleGroup.MapPost("/super-administrator", AssignSuperAdmin)
            .RequireAuthorization(DwimsPolicies.SuperAdministrator)
            .WithDisplayName("Assign Super Administrator")
            .WithSummary("Assign a Super Administrator role to a user");

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

    private static async Task<IResult> GetCurrentuser(IUserService userService, CancellationToken cancellationToken)
    {
        var result = await userService.GetCurrentUserAsync(cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Data)
            : Results.UnprocessableEntity(new
            {
                result.Error,
                result.ErrorDescription
            });
    }

    private static async Task<IResult> AssignRole(
        AssignRoleRequest request,
        IDepartmentService departmentService,
        CancellationToken cancellationToken)
    {
        if (request.GeneralRole == GeneralRole.SuperAdministrator)
            return Results.UnprocessableEntity(new
            {
                Error = "INVALID_ROLE",
                ErrorDescription = "Use /roles/super-administrator to assign Super Administrator."
            });

        var result = await departmentService.AssignRoleAsync(request.DepartmentId, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok()
            : Results.UnprocessableEntity(new
            {
                result.Error,
                result.ErrorDescription
            });
    }

    private static async Task<IResult> AssignSuperAdmin(
        AssignRoleRequest request,
        IDepartmentService departmentService,
        CancellationToken cancellationToken)
    {
        request.GeneralRole = GeneralRole.SuperAdministrator;
        var result = await departmentService.AssignRoleAsync(Guid.Empty, request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok()
            : Results.UnprocessableEntity(new
            {
                result.Error,
                result.ErrorDescription
            });
    }
}