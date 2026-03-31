using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AssignmentManagement.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;

namespace AssignmentManagement.Api.Services;

public interface IAuthService
{
    Task<RegisterPendingResponse?> RegisterAsync(RegisterRequest req);
    Task<AuthResponse?> VerifyEmailAsync(VerifyEmailRequest req);
    Task<bool> ResendVerificationAsync(ResendVerificationRequest req);
    Task ForgotPasswordAsync(ForgotPasswordRequest req);
    Task<AuthResponse?> ResetPasswordAsync(ResetPasswordRequest req);
    Task<AuthResponse?> LoginAsync(LoginRequest req);
    Task<ProfileResponse?> GetProfileAsync(int userId);
    Task<ProfileResponse?> UpdateProfileAsync(int userId, UpdateProfileRequest req);
}

public class AuthService : IAuthService
{
    private readonly IDatabaseService _db;
    private readonly IConfiguration _config;
    private readonly IEmailService _email;
    private readonly ILogger<AuthService> _logger;
    private readonly IWebHostEnvironment _env;

    public AuthService(IDatabaseService db, IConfiguration config, IEmailService email, ILogger<AuthService> logger, IWebHostEnvironment env)
    {
        _db = db;
        _config = config;
        _email = email;
        _logger = logger;
        _env = env;
    }

    public async Task<RegisterPendingResponse?> RegisterAsync(RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password) || string.IsNullOrWhiteSpace(req.FullName))
            return null;

        var emailNorm = req.Email.Trim().ToLowerInvariant();
        var hash = BCrypt.Net.BCrypt.HashPassword(req.Password, BCrypt.Net.BCrypt.GenerateSalt(12));
        var code = Random.Shared.Next(100000, 999999).ToString();
        var expires = DateTime.UtcNow.AddMinutes(15);

        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        using (var exists = new MySqlCommand("SELECT id FROM users WHERE email = @e", conn))
        {
            exists.Parameters.AddWithValue("@e", emailNorm);
            if (await exists.ExecuteScalarAsync() != null)
                return null;
        }

        using (var cmd = new MySqlCommand(
            @"INSERT INTO pending_registrations (email, full_name, password_hash, verification_code, expires_at)
              VALUES (@e, @n, @p, @c, @exp)
              ON DUPLICATE KEY UPDATE full_name = @n, password_hash = @p, verification_code = @c, expires_at = @exp", conn))
        {
            cmd.Parameters.AddWithValue("@e", emailNorm);
            cmd.Parameters.AddWithValue("@n", req.FullName.Trim());
            cmd.Parameters.AddWithValue("@p", hash);
            cmd.Parameters.AddWithValue("@c", code);
            cmd.Parameters.AddWithValue("@exp", expires);
            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex)
            {
                _logger.LogError(ex, "pending_registrations insert failed");
                return null;
            }
        }

        try
        {
            await _email.SendVerificationCodeAsync(emailNorm, req.FullName.Trim(), code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email");
            if (_email.IsConfigured)
            {
                using var delConn = _db.GetConnection();
                await delConn.OpenAsync();
                using var del = new MySqlCommand("DELETE FROM pending_registrations WHERE email = @e", delConn);
                del.Parameters.AddWithValue("@e", emailNorm);
                await del.ExecuteNonQueryAsync();
                return null;
            }
        }

        var response = new RegisterPendingResponse
        {
            Email = emailNorm,
            Message = "Verification code sent to your email. Enter it below to complete registration."
        };

        if (!_email.IsConfigured && _env.IsDevelopment())
        {
            response.DevVerificationCode = code;
            response.Message += " (Development: SMTP not configured — use the dev code shown below.)";
        }

        return response;
    }

    public async Task<AuthResponse?> VerifyEmailAsync(VerifyEmailRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Code))
            return null;

        var emailNorm = req.Email.Trim().ToLowerInvariant();
        var code = req.Code.Trim();

        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        string? fullName = null;
        string? passwordHash = null;

        using (var cmd = new MySqlCommand(
            "SELECT full_name, password_hash, expires_at FROM pending_registrations WHERE email = @e AND verification_code = @c", conn))
        {
            cmd.Parameters.AddWithValue("@e", emailNorm);
            cmd.Parameters.AddWithValue("@c", code);
            using var r = (MySqlDataReader)await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return null;
            fullName = r.GetString(r.GetOrdinal("full_name"));
            passwordHash = r.GetString(r.GetOrdinal("password_hash"));
            var exp = r.GetDateTime(r.GetOrdinal("expires_at"));
            if (exp < DateTime.UtcNow) return null;
        }

        int id;
        try
        {
            using var ins = new MySqlCommand(
                "INSERT INTO users (email, password_hash, full_name, email_verified) VALUES (@e, @p, @n, 1)", conn);
            ins.Parameters.AddWithValue("@e", emailNorm);
            ins.Parameters.AddWithValue("@p", passwordHash!);
            ins.Parameters.AddWithValue("@n", fullName!);
            await ins.ExecuteNonQueryAsync();
            id = (int)ins.LastInsertedId;
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            return null;
        }

        using (var del = new MySqlCommand("DELETE FROM pending_registrations WHERE email = @e", conn))
        {
            del.Parameters.AddWithValue("@e", emailNorm);
            await del.ExecuteNonQueryAsync();
        }

        var token = GenerateJwt(id, emailNorm, fullName!);
        return new AuthResponse { Token = token, UserId = id, Email = emailNorm, FullName = fullName! };
    }

    public async Task<bool> ResendVerificationAsync(ResendVerificationRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email)) return false;
        var emailNorm = req.Email.Trim().ToLowerInvariant();
        var code = Random.Shared.Next(100000, 999999).ToString();
        var expires = DateTime.UtcNow.AddMinutes(15);

        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(
            "UPDATE pending_registrations SET verification_code = @c, expires_at = @exp WHERE email = @e", conn);
        cmd.Parameters.AddWithValue("@c", code);
        cmd.Parameters.AddWithValue("@exp", expires);
        cmd.Parameters.AddWithValue("@e", emailNorm);
        var n = await cmd.ExecuteNonQueryAsync();
        if (n == 0) return false;

        string? fullName = null;
        using (var q = new MySqlCommand("SELECT full_name FROM pending_registrations WHERE email = @e", conn))
        {
            q.Parameters.AddWithValue("@e", emailNorm);
            fullName = (string?)await q.ExecuteScalarAsync();
        }
        if (string.IsNullOrEmpty(fullName)) return false;

        try
        {
            await _email.SendVerificationCodeAsync(emailNorm, fullName, code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resend verification email failed");
        }
        return true;
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email)) return;
        var emailNorm = req.Email.Trim().ToLowerInvariant();

        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        int userId;
        string fullName;
        using (var cmd = new MySqlCommand("SELECT id, full_name FROM users WHERE email = @e", conn))
        {
            cmd.Parameters.AddWithValue("@e", emailNorm);
            using var r = (MySqlDataReader)await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return;
            userId = r.GetInt32(r.GetOrdinal("id"));
            fullName = r.GetString(r.GetOrdinal("full_name"));
        }

        var code = Random.Shared.Next(100000, 999999).ToString();
        var expires = DateTime.UtcNow.AddMinutes(15);

        using (var ins = new MySqlCommand(
            "INSERT INTO password_reset_tokens (user_id, code, expires_at, used) VALUES (@u, @c, @exp, 0)", conn))
        {
            ins.Parameters.AddWithValue("@u", userId);
            ins.Parameters.AddWithValue("@c", code);
            ins.Parameters.AddWithValue("@exp", expires);
            await ins.ExecuteNonQueryAsync();
        }

        try
        {
            await _email.SendPasswordResetCodeAsync(emailNorm, fullName, code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password reset email failed");
        }
    }

    public async Task<AuthResponse?> ResetPasswordAsync(ResetPasswordRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Code) || string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 6)
            return null;

        var emailNorm = req.Email.Trim().ToLowerInvariant();
        var code = req.Code.Trim();

        using var conn = _db.GetConnection();
        await conn.OpenAsync();

        int userId;
        using (var cmd = new MySqlCommand("SELECT id FROM users WHERE email = @e", conn))
        {
            cmd.Parameters.AddWithValue("@e", emailNorm);
            var o = await cmd.ExecuteScalarAsync();
            if (o == null) return null;
            userId = Convert.ToInt32(o);
        }

        using (var cmd = new MySqlCommand(
            @"SELECT id FROM password_reset_tokens WHERE user_id = @u AND code = @c AND used = 0 AND expires_at > UTC_TIMESTAMP()
              ORDER BY id DESC LIMIT 1", conn))
        {
            cmd.Parameters.AddWithValue("@u", userId);
            cmd.Parameters.AddWithValue("@c", code);
            var tokenId = await cmd.ExecuteScalarAsync();
            if (tokenId == null) return null;

            var newHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword, BCrypt.Net.BCrypt.GenerateSalt(12));
            using var up = new MySqlCommand("UPDATE users SET password_hash = @p WHERE id = @id", conn);
            up.Parameters.AddWithValue("@p", newHash);
            up.Parameters.AddWithValue("@id", userId);
            await up.ExecuteNonQueryAsync();

            using var mark = new MySqlCommand("UPDATE password_reset_tokens SET used = 1 WHERE id = @id", conn);
            mark.Parameters.AddWithValue("@id", Convert.ToInt32(tokenId));
            await mark.ExecuteNonQueryAsync();
        }

        string fullName;
        using (var q = new MySqlCommand("SELECT full_name FROM users WHERE id = @id", conn))
        {
            q.Parameters.AddWithValue("@id", userId);
            fullName = (string)(await q.ExecuteScalarAsync() ?? "");
        }

        var jwt = GenerateJwt(userId, emailNorm, fullName);
        return new AuthResponse { Token = jwt, UserId = userId, Email = emailNorm, FullName = fullName };
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return null;

        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(
            @"SELECT id, email, password_hash, full_name,
              COALESCE(email_verified, 1) AS email_verified FROM users WHERE email = @e", conn);
        cmd.Parameters.AddWithValue("@e", req.Email.Trim().ToLowerInvariant());
        using var r = (MySqlDataReader)await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        var id = r.GetInt32(r.GetOrdinal("id"));
        var email = r.GetString(r.GetOrdinal("email"));
        var fullName = r.GetString(r.GetOrdinal("full_name"));
        var hash = r.GetString(r.GetOrdinal("password_hash"));
        if (!BCrypt.Net.BCrypt.Verify(req.Password, hash)) return null;
        var verifiedOrd = r.GetOrdinal("email_verified");
        if (!r.GetBoolean(verifiedOrd))
            return null;

        var token = GenerateJwt(id, email, fullName);
        return new AuthResponse { Token = token, UserId = id, Email = email, FullName = fullName };
    }

    public async Task<ProfileResponse?> GetProfileAsync(int userId)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand("SELECT id, email, full_name FROM users WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@id", userId);
        using var r = (MySqlDataReader)await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return new ProfileResponse
        {
            UserId = r.GetInt32(r.GetOrdinal("id")),
            Email = r.GetString(r.GetOrdinal("email")),
            FullName = r.GetString(r.GetOrdinal("full_name"))
        };
    }

    public async Task<ProfileResponse?> UpdateProfileAsync(int userId, UpdateProfileRequest req)
    {
        if (req == null) return null;
        var updates = new List<string>();
        var parameters = new List<(string name, object value)> { ("@id", userId) };

        if (!string.IsNullOrWhiteSpace(req.FullName))
        {
            updates.Add("full_name = @fullName");
            parameters.Add(("@fullName", req.FullName.Trim()));
        }
        if (!string.IsNullOrWhiteSpace(req.Email))
        {
            updates.Add("email = @email");
            parameters.Add(("@email", req.Email.Trim().ToLowerInvariant()));
        }
        if (!string.IsNullOrWhiteSpace(req.NewPassword))
        {
            updates.Add("password_hash = @passwordHash");
            parameters.Add(("@passwordHash", BCrypt.Net.BCrypt.HashPassword(req.NewPassword, BCrypt.Net.BCrypt.GenerateSalt(12))));
        }
        if (updates.Count == 0) return await GetProfileAsync(userId);

        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        if (!string.IsNullOrWhiteSpace(req.Email))
        {
            using var check = new MySqlCommand("SELECT id FROM users WHERE email = @email AND id != @id", conn);
            check.Parameters.AddWithValue("@email", req.Email.Trim().ToLowerInvariant());
            check.Parameters.AddWithValue("@id", userId);
            if (await check.ExecuteScalarAsync() != null)
                return null;
        }
        var sql = "UPDATE users SET " + string.Join(", ", updates) + " WHERE id = @id";
        using var cmd = new MySqlCommand(sql, conn);
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value);
        await cmd.ExecuteNonQueryAsync();
        return await GetProfileAsync(userId);
    }

    private string GenerateJwt(int userId, string email, string fullName)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "YourSecretKeyForJwtTokenMinimum32CharactersLong!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, fullName)
        };
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "AssignmentApi",
            audience: _config["Jwt:Audience"] ?? "AssignmentApp",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
