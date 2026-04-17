using System.Security.Claims;
using DWIMS.Data;
using DWIMS.Service.User;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;

namespace DWIMS.Service.Services;

public sealed class CurrentUserService (IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private readonly ClaimsPrincipal? _principal = httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var raw = _principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public IReadOnlyDictionary<Guid, GeneralRole> Roles
    {
        get
        {
            var claims = _principal?
                .FindAll(DwimsClaims.Role) ?? [];
            
            var result = new Dictionary<Guid, GeneralRole>();

            foreach (var claim in claims)
            {
                var parts = claim.Value.Split(':', 2);
                if (parts.Length == 2 &&
                    Guid.TryParse(parts[0], out var department) &&
                    Enum.TryParse<GeneralRole>(parts[1], out var role))
                {
                    if (!result.TryGetValue(department, out var existing) ||
                        existing < role)
                        result[department] = role;
                }
            }
            
            return result;
        }
        
    }
    
    public bool HasRoleInDepartment(Guid departmentId, GeneralRole role) =>
        Roles.TryGetValue(departmentId, out var departmentRole) && 
        departmentRole >= role;

    public bool isSuperAdministrator =>
        Roles.Values.Any(x => x == GeneralRole.SuperAdministrator);
}