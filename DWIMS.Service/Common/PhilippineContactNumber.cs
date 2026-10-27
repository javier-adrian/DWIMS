using System.Text.RegularExpressions;

namespace DWIMS.Service.Common;

// Optional field: empty/whitespace is always valid. When present, the number
// must be a Philippine mobile (0917 123 4567 / +63 917 123 4567 / 917 123 4567)
// or landline ((02) 8123 4567 / +63 2 8123 4567) in any common spacing.
public static class PhilippineContactNumber
{
    private static readonly Regex Pattern =
        new(@"^(?:\+?63|0)?(?:9\d{9}|[2-8]\d{8})$", RegexOptions.Compiled);

    public const string ErrorCode = "INVALID_CONTACT_NUMBER";
    public const string ErrorMessage = "Enter a valid Philippine mobile (09XX XXX XXXX) or landline number.";

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        var digits = Regex.Replace(value, @"[\s().-]", "");
        return Pattern.IsMatch(digits);
    }
}
