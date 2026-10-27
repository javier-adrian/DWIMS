using DWIMS.Data;
using DWIMS.Service.Common;
using DWIMS.Service.CurrentUser;
using DWIMS.Service.CurrentUser.Dtos;
using DWIMS.Service.CurrentUser.Requests;
using DWIMS.Service.User;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

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
                x.JobTitle,
                x.Department,
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
        user.JobTitle = request.JobTitle;
        user.Department = request.Department;
        
        if (request.BirthDate.HasValue)
            user.BirthDate = request.BirthDate.Value;
        
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
            return Result.Failure("UNAUTHORIZED", "User is not authenticated.");

        if (request.NewPassword is null || request.NewPassword.Length < 8)
            return Result.Failure("WEAK_PASSWORD", "Password must be at least 8 characters.");

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return Result.Failure("USER_NOT_FOUND", "User not found.");

        // OAuth-only accounts have no password yet; they may set one without
        // proving the current one. Password accounts must confirm theirs.
        if (user.Password is not null)
        {
            if (string.IsNullOrWhiteSpace(request.CurrentPassword) ||
                !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password))
                return Result.Failure("WRONG_PASSWORD", "Your current password is incorrect.");
        }

        user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> RegisterSignatureAsync(RegisterSignatureRequest request, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
            return Result.Failure("UNAUTHORIZED", "User is not authenticated.");

        var existing = await context.Signatures
            .Where(x => x.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);
        
        var blob = default(byte[]);
        var mimeType = default(string);

        if (request.SvgContent is { } svg)
        {
            if (!svg.TrimStart().StartsWith("<svg", StringComparison.OrdinalIgnoreCase))
                return Result.Failure("INVALID_SIGNATURE", "A valid SVG signature is required.");
            
            mimeType = "image/svg+xml";
            blob = System.Text.Encoding.UTF8.GetBytes(request.SvgContent);
        } else if (request.ImageBase64 is { } b64)
        {
            var prefix = "data:image";
            var comma = b64.IndexOf(',');
            if (b64.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && comma >= 0)
                b64 = b64[(comma + 1)..];

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(b64);
            }
            catch (FormatException)
            {
                return Result.Failure("INVALID_SIGNATURE", "A valid base64 image is required.");
            }

            if (bytes.Length is < 100 or > 3_000_000)
                return Result.Failure(
                    "INVALID_SIGNATURE",
                    "The image must be between 100 bytes and 3 Megabytes in size.");

            bool isPng = bytes.Length > 8 &&
                         bytes[0] == 0x89 &&
                         bytes[1] == 0x50 &&
                         bytes[2] == 0x4E &&
                         bytes[3] == 0x47 &&
                         bytes[4] == 0x0D &&
                         bytes[5] == 0x0A &&
                         bytes[6] == 0x1A &&
                         bytes[7] == 0x0A;

            bool isJpeg = bytes.Length > 3 &&
                          bytes[0] == 0xFF &&
                          bytes[1] == 0xD8 &&
                          bytes[2] == 0xFF;

            if (!isPng && !isJpeg)
                return Result.Failure("INVALID_SIGNATURE", "The image must be a PNG or JPEG.");

            using var bitmap = SKBitmap.Decode(bytes);

            if (bitmap is null || bitmap.Width == 0 || bitmap.Height == 0 || bitmap.Width > 4000 ||
                bitmap.Height > 4000)
                return Result.Failure("INVALID_SIGNATURE", "Image has invalid dimensions.");

            using var normalized = new MemoryStream();

            bitmap.Encode(normalized, SKEncodedImageFormat.Png, 100);
            blob = normalized.ToArray();
            mimeType = "image/png";
        }
        else
            return Result.Failure("INVALID_SIGNATURE", "The image must be a SVG, PNG or JPEG.");
        
        if (existing is not null)
        {
            existing.MimeType = mimeType;
            existing.EncryptedBlob = blob;
            existing.Created = DateTime.UtcNow;
        }
        else
        {
            context.Signatures.Add(new Signature
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                MimeType = mimeType,
                EncryptedBlob = blob,
                Created = DateTime.UtcNow,
                isCurrent = true
            });
        }

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<UserSearchResultDto>>> SearchUsersAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Result<IReadOnlyList<UserSearchResultDto>>.Success(Array.Empty<UserSearchResultDto>());

        var term = query.Trim();
        
        var users = await context.Users
            .Where(x => x.Roles.Any(r => !r.IsDeleted && r.GeneralRole >= GeneralRole.Reviewer))
            .Where(x => 
                x.FirstName.Contains(term) || 
                x.LastName.Contains(term) || 
                x.Email.Contains(term) ||
                (x.JobTitle != null && x.JobTitle.Contains(term)) ||
                (x.Department != null && x.Department.Contains(term))
                )
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
            .Take(10)
            .Select(x => new UserSearchResultDto(x.Id, x.FirstName, x.LastName, x.Email, x.JobTitle, x.Department))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<UserSearchResultDto>>.Success(users);
    }
}