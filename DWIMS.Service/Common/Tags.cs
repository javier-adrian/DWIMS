using System.Text.RegularExpressions;
using DWIMS.Data;
using DWIMS.Service.User;
using Microsoft.EntityFrameworkCore;

namespace DWIMS.Service.Common;

public static class Tags
{
    public const int MaxCount = 5;
    public const int MaxLength = 32;

    private static readonly Regex Pattern = new("^[A-Za-z0-9][A-Za-z0-9 \\-]*$", RegexOptions.Compiled);

    public static IReadOnlyList<string> Parse(string? stored) =>
        (stored ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

    public static Result<string?> Normalize(string? raw)
    {
        var tags = Parse(raw);

        if (tags.Count > MaxCount)
            return Result<string?>.Failure("INVALID_TAGS", $"At most {MaxCount} tags are allowed.");

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var kept = new List<string>();

        foreach (var tag in tags)
        {
            if (tag.Length > MaxLength)
                return Result<string?>.Failure("INVALID_TAGS", $"Each tag must be {MaxLength} characters or fewer.");
            
            if (!Pattern.IsMatch(tag))
                return Result<string?>.Failure("INVALID_TAGS", "Tags may only contain letters, digits, spaces, and hyphens.");
            
            if (seen.Add(tag))
                kept.Add(tag);
        }
        
        return Result<string?>.Success(kept.Count == 0 ? null : string.Join(",", kept));
    }

    public static bool IsVisible(string? processTags, IReadOnlyList<string> userTags)
    {
        var required = Parse(processTags);
        
        return required.Count == 0 ||
               required.Any(t => userTags.Contains(t, StringComparer.OrdinalIgnoreCase));
    }

    public static async Task<bool> CallerCanSeeAsync(
        AppDbContext context,
        ICurrentUserService currentUser,
        Data.Process process,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.isAdministratorOrAbove)
            return true;

        if (string.IsNullOrWhiteSpace(process.Tags))
            return true;

        if (currentUser.UserId == null)
            return false;

        var stored = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == currentUser.UserId)
            .Select(u => u.Tags)
            .FirstOrDefaultAsync(cancellationToken);
        
        return IsVisible(process.Tags, Parse(stored));
    }
}