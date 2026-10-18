using DWIMS.Service.Admin.Dtos;
using DWIMS.Service.Admin.Requests;
using DWIMS.Service.Common;

namespace DWIMS.Service.Admin;

public interface IUserAdministrationService
{
    Task<Result<PagedResult<UserDirectoryDto>>> GetUsersAsync(UserFilterRequest request, CancellationToken cancellationToken);
    Task<Result<UserDirectoryDto>> GetUserAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<Guid>> DeleteUserAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<Guid>> ReactivateUserAsync(Guid id, CancellationToken cancellationToken);
}