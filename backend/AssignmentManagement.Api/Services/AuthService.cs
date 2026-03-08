using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AssignmentManagement.Api.Models;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;

namespace AssignmentManagement.Api.Services;

public interface IAuthService
{
    Task<AuthResponse?> RegisterAsync(RegisterRequest req);
    Task<AuthResponse?> LoginAsync(LoginRequest req);
}

public class AuthService : IAuthService
{
    private readonly IDatabaseService _db;
    private readonly IConfiguration _config;

    public AuthService(IDatabaseService db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password) || string.IsNullOrWhiteSpace(req.FullName))
            return null;

        var hash = BCrypt.Net.BCrypt.HashPassword(req.Password, BCrypt.Net.BCrypt.GenerateSalt(12));
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(
            "INSERT INTO users (email, password_hash, full_name) VALUES (@e, @p, @n)", conn);
        cmd.Parameters.AddWithValue("@e", req.Email.Trim().ToLowerInvariant());
        cmd.Parameters.AddWithValue("@p", hash);
        cmd.Parameters.AddWithValue("@n", req.FullName.Trim());
        try
        {
            await cmd.ExecuteNonQueryAsync();
            var id = (int)cmd.LastInsertedId;
            var token = GenerateJwt(id, req.Email, req.FullName);
            return new AuthResponse { Token = token, UserId = id, Email = req.Email, FullName = req.FullName.Trim() };
        }
        catch (MySqlException ex) when (ex.Number == 1062) // duplicate email
        {
            return null;
        }
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return null;

        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(
            "SELECT id, email, password_hash, full_name FROM users WHERE email = @e", conn);
        cmd.Parameters.AddWithValue("@e", req.Email.Trim().ToLowerInvariant());
        using var r = (MySqlDataReader)await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        var id = r.GetInt32(r.GetOrdinal("id"));
        var email = r.GetString(r.GetOrdinal("email"));
        var fullName = r.GetString(r.GetOrdinal("full_name"));
        var hash = r.GetString(r.GetOrdinal("password_hash"));
        if (!BCrypt.Net.BCrypt.Verify(req.Password, hash)) return null;
        var token = GenerateJwt(id, email, fullName);
        return new AuthResponse { Token = token, UserId = id, Email = email, FullName = fullName };
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
