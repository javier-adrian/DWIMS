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

        group.MapGet("/", GetDepartment);
        
        group.MapPost("/", CreateDepartment)
            .RequireAuthorization(DwimsPolicies.SuperAdministrator);
        
        group.MapPut("/{id:guid}", UpdateDepartment)
            .RequireAuthorization(DwimsPolicies.SuperAdministrator);
        
        group.MapDelete("/{id:guid}", DeleteDepartment)
            .RequireAuthorization(DwimsPolicies.SuperAdministrator);
        
        group.MapGet("/{id:guid}/members", GetMembers)
            .RequireAuthorization(DwimsPolicies.Administrator);
        
        return app;
    }

    private static Task GetDepartment(HttpContext context)
    {
        throw new NotImplementedException();
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