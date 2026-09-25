using System.Text.Json;
using FleetPulse.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FleetPulse.Infrastructure.Services;

/// <summary>
/// Verifies Google ID tokens using Google's tokeninfo endpoint. Google performs the
/// cryptographic signature/expiry/issuer validation server-side; this client additionally
/// enforces that the token's audience matches our OAuth Client ID and that the email is verified.
/// </summary>
public class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly HttpClient _http;
    private readonly GoogleAuthOptions _options;
    private readonly ILogger<GoogleTokenValidator> _logger;

    public GoogleTokenValidator(
        HttpClient http,
        IOptions<GoogleAuthOptions> options,
        ILogger<GoogleTokenValidator> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GoogleIdentity?> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken) || string.IsNullOrWhiteSpace(_options.ClientId))
            return null;

        try
        {
            var baseUrl = string.IsNullOrWhiteSpace(_options.TokenInfoUrl)
                ? "https://oauth2.googleapis.com/tokeninfo"
                : _options.TokenInfoUrl.TrimEnd('/');

            using var response = await _http.GetAsync($"{baseUrl}?id_token={Uri.EscapeDataString(idToken)}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google tokeninfo rejected the ID token (HTTP {StatusCode}).", (int)response.StatusCode);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = doc.RootElement;

            // Google tokeninfo already returns a failure for expired/garbage tokens; these
            // checks are local enforcement so a compromised key can't mint arbitrary identities.
            if (!root.TryGetProperty("aud", out var aud) || aud.GetString() != _options.ClientId)
                return null;
            if (!root.TryGetProperty("email_verified", out var verified) || verified.GetString() != "true")
                return null;

            var issuer = root.TryGetProperty("iss", out var iss) ? iss.GetString() : null;
            if (issuer is not ("accounts.google.com" or "https://accounts.google.com"))
                return null;

            var email = root.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
            if (string.IsNullOrWhiteSpace(email))
                return null;

            var subject = root.TryGetProperty("sub", out var subProp) ? subProp.GetString() : null;
            var fullName = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;

            return new GoogleIdentity(subject ?? email, email, fullName ?? string.Empty);
        }
        catch (Exception ex)
        {
            // A Google outage must never block the API — treat as "sign-in failed".
            _logger.LogWarning(ex, "Google token validation failed.");
            return null;
        }
    }
}