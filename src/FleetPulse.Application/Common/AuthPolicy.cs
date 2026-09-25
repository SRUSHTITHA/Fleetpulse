namespace FleetPulse.Application.Common;

public static class AuthPolicy
{
    public const int MinimumPasswordLength = 8;

    public static bool IsValidPassword(string? password) =>
        !string.IsNullOrWhiteSpace(password) && password.Length >= MinimumPasswordLength;
}
