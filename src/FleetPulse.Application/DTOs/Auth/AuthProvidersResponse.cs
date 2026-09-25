namespace FleetPulse.Application.DTOs.Auth;

public class AuthProvidersResponse
{
    /// <summary>Google OAuth Client ID, or null when Google sign-in is not configured.</summary>
    public string? GoogleClientId { get; set; }
}