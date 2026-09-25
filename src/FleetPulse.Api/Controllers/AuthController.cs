using FleetPulse.Application.DTOs.Auth;
using FleetPulse.Application.Common;
using FleetPulse.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FleetPulse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;
    private readonly IOptions<GoogleAuthOptions> _google;
    private readonly SmtpOptions _smtp;

    public AuthController(IAuthService authService, IOptions<GoogleAuthOptions> google, IOptions<SmtpOptions> smtp)
    {
        _authService = authService;
        _google = google;
        _smtp = smtp.Value;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (result is null)
            return Unauthorized(new { message = "Invalid email or password." });
        return Ok(result);
    }

    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> GoogleLogin(GoogleLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(_google.Value.ClientId))
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Google sign-in is not configured." });

        var result = await _authService.LoginWithGoogleAsync(request);
        if (result is null)
            return Unauthorized(new { message = "Google sign-in failed." });
        return Ok(result);
    }

    [HttpGet("providers")]
    [AllowAnonymous]
    public ActionResult<AuthProvidersResponse> Providers()
        => Ok(new AuthProvidersResponse
        {
            GoogleClientId = string.IsNullOrWhiteSpace(_google.Value.ClientId) ? null : _google.Value.ClientId
        });

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me()
    {
        var userId = CurrentUserId();
        if (userId is null)
            return Unauthorized();

        var user = await _authService.GetUserByIdAsync(userId.Value);
        if (user is null)
            return NotFound();
        return Ok(user);
    }

    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> Users()
        => Ok(await _authService.GetAllUsersAsync());

    [HttpPut("users/{id:guid}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> UpdateRole(Guid id, UpdateUserRoleRequest request)
    {
        var currentId = CurrentUserId();
        if (currentId is not null && currentId.Value == id)
            return BadRequest(new { message = "You cannot change your own role." });

        var result = await _authService.UpdateUserRoleAsync(id, request.Role);
        if (result is null)
            return NotFound(new { message = "User not found." });
        return Ok(result);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        if (!_smtp.IsConfigured)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Password reset is not configured. Please contact support." });

        await _authService.RequestPasswordResetAsync(request.Email);
        return Ok(new { message = "If that email is registered, a password reset link has been sent." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        if (!_smtp.IsConfigured)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Password reset is not configured. Please contact support." });

        var ok = await _authService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword);
        if (!ok)
            return BadRequest(new { message = "This reset link is invalid or has expired. Please request a new one." });

        return Ok(new { message = "Your password has been reset. You can sign in now." });
    }

    [HttpPost("signup")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> SignUp(SignUpRequest request)
    {
        var result = await _authService.SignUpAsync(request);
        if (result is null)
        {
            if (!EmailNormalizer.IsValid(request.Email) || !AuthPolicy.IsValidPassword(request.Password))
                return BadRequest(new { message = "Please enter a valid email address and a password of at least 8 characters." });
            return Conflict(new { message = "Sign up failed — that email may already be registered." });
        }
        return Ok(result);
    }
}