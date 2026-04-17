using DWIMS.Data;
using Microsoft.AspNetCore.Authorization;

namespace DWIMS.Service;

public sealed class Requirement(GeneralRole role) : IAuthorizationRequirement
{
    public GeneralRole Role { get; } = role;
}