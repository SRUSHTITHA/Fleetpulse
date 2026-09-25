namespace FleetPulse.Application.DTOs.Auth;

public class GoogleLoginRequest
{
    /// <summary>The Google ID token (JWT) produced by Google Identity Services' credential callback.</summary>
    public string IdToken { get; set; } = string.Empty;
}