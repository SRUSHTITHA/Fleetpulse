using System.ComponentModel.DataAnnotations;

namespace FleetPulse.Application.DTOs.Auth;

public class SignUpRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required.")]
    public string FullName { get; set; } = string.Empty;

    public string? LicenseNumber { get; set; }
}