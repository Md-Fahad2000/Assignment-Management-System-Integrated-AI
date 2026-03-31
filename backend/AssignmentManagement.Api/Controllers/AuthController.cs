using System.Security.Claims;
using AssignmentManagement.Api.Models;
using AssignmentManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest model)
    {
        var result = await _auth.RegisterAsync(model);
        if (result == null)
            return BadRequest(new { message = "Email already registered, invalid input, or verification email could not be sent. Check SMTP settings." });
        return Ok(result);
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest model)
    {
        var result = await _auth.VerifyEmailAsync(model);
        if (result == null)
            return BadRequest(new { message = "Invalid or expired code, or email already registered." });
        return Ok(result);
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest model)
    {
        var ok = await _auth.ResendVerificationAsync(model);
        if (!ok)
            return BadRequest(new { message = "No pending registration found for this email." });
        return Ok(new { message = "If an account is pending, a new code was sent." });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest model)
    {
        await _auth.ForgotPasswordAsync(model);
        return Ok(new { message = "If an account exists for this email, you will receive a reset code." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest model)
    {
        var result = await _auth.ResetPasswordAsync(model);
        if (result == null)
            return BadRequest(new { message = "Invalid or expired code, or password too short (min 6 characters)." });
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest model)
    {
        var result = await _auth.LoginAsync(model);
        if (result == null)
            return Unauthorized(new { message = "Invalid email or password, or email not verified." });
        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        var profile = await _auth.GetProfileAsync(userId);
        if (profile == null) return NotFound();
        return Ok(profile);
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest model)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        var profile = await _auth.UpdateProfileAsync(userId, model);
        if (profile == null)
            return BadRequest(new { message = "Invalid input or email already in use." });
        return Ok(profile);
    }
}
