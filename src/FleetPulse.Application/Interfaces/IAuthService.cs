using FleetPulse.Application.DTOs.Auth;
using FleetPulse.Domain.Enums;

namespace FleetPulse.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<LoginResponse?> LoginWithGoogleAsync(GoogleLoginRequest request, CancellationToken cancellationToken = default);
    Task<LoginResponse?> SignUpAsync(SignUpRequest request, CancellationToken cancellationToken = default);
    Task<bool> RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserByIdAsync(Guid id);
    Task<IReadOnlyList<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<UserDto?> UpdateUserRoleAsync(Guid userId, UserRole role, CancellationToken cancellationToken = default);
}