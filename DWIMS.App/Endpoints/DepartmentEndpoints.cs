using DWIMS.Service.Auth;
using DWIMS.Service.Common;
using DWIMS.Service.Department;
using DWIMS.Service.Department.Requests;
using DWIMS.Service.Services;

namespace DWIMS.Controllers;

public static class DepartmentEndpoints
{
    public static IEndpointRouteBuilder MapDepartmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("department")
            .WithTags("Department")
            .RequireAuthorization();

        group.MapGet("/", GetDepartment)
            .WithDisplayName("Get Departments")
            .WithSummary("Get all departments the current user has access to");

        group.MapPost("/", CreateDepartment)
            .WithDisplayName("Create Department")
            .WithSummary("Create a new department")
            .RequireAuthorization(DwimsPolicies.SuperAdministrator);

        group.MapPut("/{id:guid}", UpdateDepartment)
            .WithDisplayName("Update Department")
            .WithSummary("Update an existing department")
            .RequireAuthorization(DwimsPolicies.SuperAdministrator);

        group.MapDelete("/{id:guid}", DeleteDepartment)
            .WithDisplayName("Delete Department")
            .WithSummary("Delete a department")
            .RequireAuthorization(DwimsPolicies.SuperAdministrator);

        group.MapGet("/{id:guid}/members", GetMembers)
            .WithDisplayName("Get Department Members")
            .WithSummary("Get all members of a department")
            .RequireAuthorization(DwimsPolicies.Administrator);
        
        return app;
    }

    private static async Task<IResult> GetDepartment(
        IDepartmentService departmentService, 
        CancellationToken cancellationToken)
    {
        var result = await departmentService.GetDepartmentAsync(cancellationToken);
        return result.ToOkResult();
    }

    private static async Task<IResult> CreateDepartment(
        CreateDepartmentRequest request,
        IDepartmentService departmentService,
        CancellationToken cancellationToken)
    {
        var result = await departmentService.CreateDepartmentAsync(request, cancellationToken);
        return result.ToCreatedResult("/department/");
    }

    private static Task UpdateDepartment(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static Task DeleteDepartment(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static async Task<IResult> GetMembers(
        Guid departmentId,
        IDepartmentService departmentService,
        CancellationToken cancellationToken)
    {
        var result = await departmentService.GetMembersAsync(departmentId, cancellationToken);
        return result.ToOkResult();
    }
}