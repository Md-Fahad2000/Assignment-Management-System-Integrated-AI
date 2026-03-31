namespace AssignmentManagement.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string FullName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class RegisterRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string FullName { get; set; } = "";
}

public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class AuthResponse
{
    public string Token { get; set; } = "";
    public int UserId { get; set; }
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
}

public class ProfileResponse
{
    public int UserId { get; set; }
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
}

public class UpdateProfileRequest
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? NewPassword { get; set; }
}

public class RegisterPendingResponse
{
    public string Message { get; set; } = "";
    public string Email { get; set; } = "";
    /// <summary>Only in Development when SMTP is not configured — for testing.</summary>
    public string? DevVerificationCode { get; set; }
}

public class VerifyEmailRequest
{
    public string Email { get; set; } = "";
    public string Code { get; set; } = "";
}

public class ResendVerificationRequest
{
    public string Email { get; set; } = "";
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = "";
}

public class ResetPasswordRequest
{
    public string Email { get; set; } = "";
    public string Code { get; set; } = "";
    public string NewPassword { get; set; } = "";
}
