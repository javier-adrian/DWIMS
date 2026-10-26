using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace DWIMS.Service.Common;

public static class FieldPatterns
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMilliseconds(250);
    private static readonly ConcurrentDictionary<string, Regex> Cache = new();

    public static bool IsValid(string? pattern, string value)
    {
        if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(value))
            return true;
        
        try
        {
            var regex = Cache.GetOrAdd(pattern, p => new Regex($"^(?:{p})$", RegexOptions.Compiled, Timeout));
            return regex.IsMatch(value);
        }
        catch (RegexMatchTimeoutException) { return false; }
        catch (ArgumentException) { return false; }
    }

    public static string? Validate(string? pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return null;
        
        try { 
            _ = new Regex($"^(?:{pattern})$", RegexOptions.None, Timeout);
            return null;
        } catch (ArgumentException ex) { return ex.Message; }
    }
}