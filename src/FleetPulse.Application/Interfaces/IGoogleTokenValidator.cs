namespace FleetPulse.Application.Interfaces;

public sealed record GoogleIdentity(string Subject, string Email, string FullName);

public interface IGoogleTokenValidator
{
    /// <summary>
    /// Verifies a Google ID token (signature, expiry and audience via Google's tokeninfo
    /// endpoint) and returns the parsed identity, or null when the token is invalid or
    /// Google auth is not configured.
    /// </summary>
    Task<GoogleIdentity?> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}