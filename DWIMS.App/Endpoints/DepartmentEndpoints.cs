using DWIMS.Service.Auth;

namespace DWIMS.Controllers;

public static class DepartmentEndpoints
{
    public static IEndpointRouteBuilder MapDepartmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("department")
            .WithTags("Department")
            .RequireAuthorization(DwimsPolicies.SuperAdministrator);

        group.MapGet("/", GetDepartment);
        
        group.MapPost("/", CreateDepartment)
            .RequireAuthorization(DwimsPolicies.SuperAdministrator);
        
        group.MapPut("/{id:guid}", UpdateDepartment)
            .RequireAuthorization(DwimsPolicies.SuperAdministrator);
        
        group.MapDelete("/{id:guid}", DeleteDepartment)
            .RequireAuthorization(DwimsPolicies.SuperAdministrator);
        
        group.MapGet("/{id:guid}/members", GetMembers)
            .RequireAuthorization(DwimsPolicies.Administrator);
        
        group.MapPost("/{id:guid}/members", AssignRole)
            .RequireAuthorization(DwimsPolicies.Administrator);

        

        return app;
    }

    private static Task GetDepartment(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static Task CreateDepartment(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static Task UpdateDepartment(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static Task DeleteDepartment(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static Task GetMembers(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static Task AssignRole(HttpContext context)
    {
        throw new NotImplementedException();
    }
}