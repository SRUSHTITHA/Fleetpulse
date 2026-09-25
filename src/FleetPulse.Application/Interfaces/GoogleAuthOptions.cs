namespace FleetPulse.Application.Interfaces;

public class GoogleAuthOptions
{
    public const string SectionName = "GoogleAuth";

    /// <summary>
    /// Google OAuth 2.0 Client ID (Web application) used to verify the audience of
    /// ID tokens issued by Google Identity Services. When empty, Google sign-in is disabled.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>Google endpoint used to verify the ID token (public tokeninfo API).</summary>
    public string TokenInfoUrl { get; set; } = "https://oauth2.googleapis.com/tokeninfo";
}