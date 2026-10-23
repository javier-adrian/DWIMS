using DWIMS.Data;
using DWIMS.Service.Admin;
using DWIMS.Service.Admin.Requests;
using DWIMS.Service.Auth;
using DWIMS.Service.CurrentUser;
using DWIMS.Service.CurrentUser.Requests;
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

        group.MapPut("/me/password", ChangePassword)
            .WithDisplayName("Change Current User Password")
            .WithSummary("Set or change the current user's password");

        group.MapDelete("/{userId:guid}", DeleteUser)
            .WithDisplayName("Delete User")
            .WithSummary("Delete a user by ID")
            .RequireAuthorization(DwimsPolicies.SuperAdministrator);
        
        group.MapPost("/{userId:guid}/restore", ReactivateUser)
            .WithDisplayName("Reactivate User")
            .WithSummary("Reactivate a deleted user by ID")
            .RequireAuthorization(DwimsPolicies.SuperAdministrator);
        
        group.MapPut("/me/signature", UploadSignature)
            .WithDisplayName("Upload Signature")
            .WithSummary("Upload a signature for the current user");
        
        group.MapGet("", GetUsers)
            .WithDisplayName("Get Users")
            .WithSummary("Get a list of users")
            .RequireAuthorization(DwimsPolicies.SuperAdministrator);

        group.MapGet("/search", SearchUsers)
            .WithDisplayName("Search Users")
            .WithSummary("Search for users by name, email, job title, or department");
        
        group.MapGet("/{userId:guid}", GetUser)
            .WithDisplayName("Get User")
            .WithSummary("Get a user by ID")
            .RequireAuthorization(DwimsPolicies.SuperAdministrator);

        var roleGroup = app.MapGroup("roles")
            .WithTags("Roles")
            .RequireAuthorization(DwimsPolicies.SuperAdministrator);

        roleGroup.MapPost("/", AssignRole)
            .WithDisplayName("Assign Role")
            .WithSummary("Assign a role to a user");

        roleGroup.MapDelete("/{id:guid}", RemoveRole)
            .WithDisplayName("Remove Role")
            .WithSummary("Remove a role from a user");

        roleGroup.MapPost("/super-administrator", AssignSuperAdmin)
            .RequireAuthorization(DwimsPolicies.SuperAdministrator)
            .WithDisplayName("Assign Super Administrator")
            .WithSummary("Assign a Super Administrator role to a user");

        return app;
    }

    private static async Task<IResult> SearchUsers(
        string? query,
        IUserService userService,
        CancellationToken cancellationToken)
    {
        var result = await userService.SearchUsersAsync(query ?? string.Empty, cancellationToken);

        return result.ToOkResult();
    }

    private static async Task<IResult> ReactivateUser(
        Guid userId,
        IUserAdministrationService userAdministrationService, 
        CancellationToken cancellationToken)
    {
        var result = await userAdministrationService.ReactivateUserAsync(userId, cancellationToken);

        return result.ToOkResult();
    }

    private static async Task<IResult> DeleteUser(
        Guid userId,
        IUserAdministrationService userAdministrationService,
        CancellationToken cancellationToken)
    {
        var result = await userAdministrationService.DeleteUserAsync(userId, cancellationToken);

        return result.ToOkResult();
    }

    private static async Task<IResult> GetUser(
        Guid userId,
        IUserAdministrationService userAdministrationService, 
        CancellationToken cancellationToken)
    {
        var result = await userAdministrationService.GetUserAsync(userId, cancellationToken);

        return result.ToOkResult();
    }

    private static async Task<IResult> GetUsers(
        [AsParameters] UserFilterRequest request, 
        IUserAdministrationService userAdministrationService, 
        CancellationToken cancellationToken)
    {
        var result = await userAdministrationService.GetUsersAsync(request, cancellationToken);
        
        return result.ToOkResult();
    }

    private static async Task<IResult> UploadSignature(RegisterSignatureRequest request, IUserService userService, CancellationToken cancellationToken)
    {
        var result = await userService.RegisterSignatureAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok()
            : Results.UnprocessableEntity(new { result.Error, result.ErrorDescription });
    }

    private static async Task<IResult> UpdateCurrentuser(
        UpdateProfileRequest request,
        IUserService userService,
        CancellationToken cancellationToken)
    {
        var result = await userService.UpdateProfileAsync(request, cancellationToken);

        return result.ToOkResult();
    }

    private static async Task<IResult> ChangePassword(
        ChangePasswordRequest request,
        IUserService userService,
        CancellationToken cancellationToken)
    {
        var result = await userService.ChangePasswordAsync(request, cancellationToken);

        return result.IsSuccess
            ? Results.Ok()
            : Results.UnprocessableEntity(new { result.Error, result.ErrorDescription });
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
        // if (request.GeneralRole == GeneralRole.Administrator)
        //     return Results.UnprocessableEntity(new
        //     {
        //         Error = "ROLE_RETIRED",
        //         ErrorDescription = "The administrator role is no longer used. Grant reviewer or super administrator instead."
        //     });
        
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

    private static async Task<IResult> RemoveRole(
        Guid id,
        IDepartmentService departmentService,
        CancellationToken cancellationToken)
    {
        var result = await departmentService.RemoveRoleAsync(id, cancellationToken);

        return result.IsSuccess
            ? Results.Ok()
            : Results.UnprocessableEntity(new
            {
                result.Error,
                result.ErrorDescription
            });
    }
}