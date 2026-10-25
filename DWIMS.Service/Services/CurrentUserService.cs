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
                if (Enum.TryParse<GeneralRole>(claim.Value, out var globalRole))
                {
                    if (!result.TryGetValue(Guid.Empty, out var existingGlobal) || existingGlobal < globalRole) 
                        result[Guid.Empty] = globalRole;
                    continue;
                }

                var parts = claim.Value;
                if (parts.Contains(':') &&
                    Guid.TryParse(parts.Split(':')[0], out var department) &&
                    Enum.TryParse<GeneralRole>(parts.Split(':')[1], out var role))
                {
                    if (!result.TryGetValue(department, out var existing) ||
                        existing < role)
                        result[department] = role;
                }
            }

            return result;
        }

    }
    
    public bool isSuperAdministrator =>
        Roles.Values.Any(x => x == GeneralRole.SuperAdministrator);
}