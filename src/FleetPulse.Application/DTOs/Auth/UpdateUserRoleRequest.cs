using FleetPulse.Domain.Enums;

namespace FleetPulse.Application.DTOs.Auth;

public class UpdateUserRoleRequest
{
    public UserRole Role { get; set; }
}