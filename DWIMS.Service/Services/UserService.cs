using DWIMS.Data;
using DWIMS.Service.Common;
using DWIMS.Service.CurrentUser;
using DWIMS.Service.CurrentUser.Dtos;
using DWIMS.Service.CurrentUser.Requests;
using DWIMS.Service.User;
using Microsoft.EntityFrameworkCore;

namespace DWIMS.Service.Services;

public class UserService(
    AppDbContext context, 
    ICurrentUserService currentUser) : IUserService
{
    public async Task<Result<UserProfileDto>> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var id = currentUser.UserId;
        
        var user = await context.Users
            .Where(x => x.Id == id)
            .Select(x => new UserProfileDto(
                x.Id,
                x.FirstName,
                x.MiddleName,
                x.LastName,
                x.Email,
                x.ContactNumber,
                context.Signatures.Any(y => y.UserId == id)))
            .FirstOrDefaultAsync(cancellationToken);
        
        if (user is null)
            return Result<UserProfileDto>.Failure("USER_NOT_FOUND", $"User not found. {id}");
        
        return Result<UserProfileDto>.Success(user);
    }

    public Task<Result> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}