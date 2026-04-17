using DWIMS.Service.Common;
using DWIMS.Service.CurrentUser.Dtos;
using DWIMS.Service.CurrentUser.Requests;

namespace DWIMS.Service.CurrentUser;

public interface IUserService
{
    Task<Result<UserProfileDto>> GetCurrentUserAsync(CancellationToken cancellationToken = default);
    
    Task<Result> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default);
}