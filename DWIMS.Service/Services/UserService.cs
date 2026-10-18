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

    public async Task<Result> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
            return Result.Failure("UNAUTHORIZED", "User is not authenticated.");
            
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        
        if (user is null)
            return Result.Failure("USER_NOT_FOUND", "User not found.");
        
        if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase) &&
            await context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == request.Email && u.Id != userId, cancellationToken))
            return Result.Failure("EMAIL_TAKEN", "An account with this email already exists.");
        
        user.FirstName = request.FirstName;
        user.MiddleName = request.MiddleName ?? "";
        user.LastName = request.LastName;
        user.Email = request.Email;
        user.ContactNumber = request.ContactNumber;
        
        if (request.BirthDate.HasValue)
            user.BirthDate = request.BirthDate.Value;
        
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> RegisterSignatureAsync(RegisterSignatureRequest request, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
            return Result.Failure("UNAUTHORIZED", "User is not authenticated.");

        if (string.IsNullOrWhiteSpace(request.SvgContent) || !request.SvgContent.TrimStart().StartsWith("<svg", StringComparison.OrdinalIgnoreCase))
            return Result.Failure("INVALID_SIGNATURE", "A valid SVG signature is required.");

        var existing = await context.Signatures
            .Where(x => x.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            existing.MimeType = "image/svg+xml";
            existing.EncryptedBlob = System.Text.Encoding.UTF8.GetBytes(request.SvgContent);
            existing.Created = DateTime.UtcNow;
        }
        else
        {
            context.Signatures.Add(new Signature
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                MimeType = "image/svg+xml",
                EncryptedBlob = System.Text.Encoding.UTF8.GetBytes(request.SvgContent),
                Created = DateTime.UtcNow,
                isCurrent = true
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}