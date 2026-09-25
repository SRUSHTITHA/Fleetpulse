using System.Text.RegularExpressions;

namespace FleetPulse.Application.Common;

public static class EmailNormalizer
{
    public static string Normalize(string? email) =>
        (email ?? string.Empty).Trim().ToLowerInvariant();

    public static bool IsValid(string? email)
    {
        var normalized = Normalize(email);
        return normalized.Length <= 254 &&
               Regex.IsMatch(normalized, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }
}
