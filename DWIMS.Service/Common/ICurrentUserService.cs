using DWIMS.Data;

namespace DWIMS.Service.User;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    IReadOnlyDictionary<Guid, GeneralRole> Roles { get; }
    bool HasRoleInDepartment(Guid departmentId, GeneralRole role);
    bool isSuperAdministrator { get; }
}