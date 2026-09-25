using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FleetPulse.Application.DTOs.Auth;
using FleetPulse.Application.Interfaces;
using FleetPulse.Application.Interfaces.Repositories;
using FleetPulse.Domain.Entities;
using FleetPulse.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace FleetPulse.Infrastructure.Services;

public class AuthService : IAuthService
{
    private const int ResetTokenLifetimeMinutes = 60;

    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGoogleTokenValidator _google;
    private readonly IEmailService _email;
    private readonly WebAppOptions _webApp;
    private readonly JwtOptions _jwt;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IUnitOfWork unitOfWork,
        IOptions<JwtOptions> jwt,
        IGoogleTokenValidator google,
        IEmailService email,
        IOptions<WebAppOptions> webApp,
        ILogger<AuthService> logger)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _google = google;
        _email = email;
        _webApp = webApp.Value;
        _jwt = jwt.Value;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _users.GetByEmailAsync(request.Email, includeDriver: true);

        if (user is null)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        return BuildLogin(user);
    }

    public async Task<LoginResponse?> LoginWithGoogleAsync(GoogleLoginRequest request, CancellationToken cancellationToken = default)
    {
        var identity = await _google.ValidateAsync(request.IdToken, cancellationToken);
        if (identity is null)
            return null;

        var user = await _users.GetByEmailAsync(identity.Email, includeDriver: true, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Email = identity.Email.Trim().ToLower(),
                FullName = string.IsNullOrWhiteSpace(identity.FullName) ? identity.Email : identity.FullName,
                Role = UserRole.Driver,
                // A random BCrypt hash so the password-login path always rejects; this
                // user can only authenticate through Google.
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")),
                Driver = new Driver
                {
                    LicenseNumber = "DL-" + Guid.NewGuid().ToString("N")[..8]
                }
            };

            await _users.AddAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return BuildLogin(user);
    }

    public async Task<LoginResponse?> SignUpAsync(SignUpRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !IsValidEmail(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.FullName))
            return null;

        if (await _users.ExistsByEmailAsync(request.Email, cancellationToken))
            return null;

        var user = new User
        {
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName.Trim(),
            Role = UserRole.Driver
        };

        user.Driver = new Driver
        {
            UserId = user.Id,
            LicenseNumber = string.IsNullOrWhiteSpace(request.LicenseNumber)
                ? "DL-" + Guid.NewGuid().ToString("N")[..8]
                : request.LicenseNumber.Trim()
        };

        await _users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return BuildLogin(user);
    }

    public async Task<bool> RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || !IsValidEmail(normalized))
            return false;

        var user = await _users.GetByEmailAsync(normalized, cancellationToken: cancellationToken);
        if (user is null)
            return false;

        var token = CreatePasswordResetToken(normalized);
        var link = _webApp.ResetPasswordUrl(normalized, token);

        try
        {
            await _email.SendAsync(
                user.Email,
                "Reset your FleetPulse password",
                BuildResetEmail(user.FullName, link),
                user.FullName,
                cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password-reset email to {Email}.", user.Email);
            return false;
        }
    }

    public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default)
    {
        var normalized = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized) ||
            string.IsNullOrWhiteSpace(token) ||
            string.IsNullOrWhiteSpace(newPassword) ||
            newPassword.Length < 8)
            return false;

        if (!ValidatePasswordResetToken(normalized, token))
            return false;

        var user = await _users.GetByEmailAsync(normalized, cancellationToken: cancellationToken);
        if (user is null)
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id)
    {
        var user = await _users.GetByIdAsync(id, includeDriver: true);
        return user is null ? null : MapUser(user);
    }

    public async Task<IReadOnlyList<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _users.GetAllAsync(includeDriver: true, cancellationToken);
        return users.Select(MapUser).ToList();
    }

    public async Task<UserDto?> UpdateUserRoleAsync(Guid userId, UserRole role, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, includeDriver: true, cancellationToken);
        if (user is null)
            return null;

        user.Role = role;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapUser(user);
    }

    private string GenerateToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserDto MapUser(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FullName = user.FullName,
        Role = user.Role,
        DriverId = user.Driver?.Id
    };

    private LoginResponse BuildLogin(User user) => new()
    {
        Token = GenerateToken(user),
        ExpiresAt = DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes),
        User = MapUser(user)
    };

    public static bool IsValidEmail(string email) =>
        email.Length <= 254 && System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    private string CreatePasswordResetToken(string email)
        => CreatePasswordResetToken(email, DateTime.UtcNow.AddMinutes(ResetTokenLifetimeMinutes));

    private string CreatePasswordResetToken(string email, DateTime expiryUtc)
    {
        var payload = $"{email}|{expiryUtc.Ticks}";
        var signature = Sign(payload);
        return Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(payload)) + "." + Base64UrlEncoder.Encode(signature);
    }

    private bool ValidatePasswordResetToken(string email, string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 2)
            return false;

        byte[] payload;
        byte[] signature;
        try
        {
            payload = Base64UrlEncoder.DecodeBytes(parts[0]);
            signature = Base64UrlEncoder.DecodeBytes(parts[1]);
        }
        catch (FormatException)
        {
            return false;
        }

        if (!FixedTimeEquals(signature, Sign(Encoding.UTF8.GetString(payload))))
            return false;

        var pieces = Encoding.UTF8.GetString(payload).Split('|');
        if (pieces.Length != 2 || !string.Equals(pieces[0], email, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!long.TryParse(pieces[1], out var ticks))
            return false;

        var expiryUtc = new DateTime(ticks, DateTimeKind.Utc);
        return expiryUtc >= DateTime.UtcNow;
    }

    private byte[] Sign(string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_jwt.Key));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
    }

    private static bool FixedTimeEquals(byte[] a, byte[] b)
        => a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);

    private static string BuildResetEmail(string fullName, string resetLink) => $"""
        <div style="font-family:Arial,Helvetica,sans-serif;max-width:480px;margin:0 auto;color:#1f2937;">
          <h2 style="margin:0 0 12px;">Reset your FleetPulse password</h2>
          <p>Hi {fullName},</p>
          <p>We received a request to reset your password. The link below is valid for {ResetTokenLifetimeMinutes} minutes.</p>
          <p><a href="{resetLink}" style="display:inline-block;padding:10px 18px;background:#2563eb;color:#ffffff;text-decoration:none;border-radius:6px;">Reset password</a></p>
          <p style="color:#6b7280;font-size:13px;">If you didn't request this, you can safely ignore this email.</p>
        </div>
        """;
}