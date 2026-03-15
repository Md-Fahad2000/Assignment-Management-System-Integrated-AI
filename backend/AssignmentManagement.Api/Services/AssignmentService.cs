using System.Data;
using AssignmentManagement.Api.Models;
using MySql.Data.MySqlClient;

namespace AssignmentManagement.Api.Services;

public interface IAssignmentService
{
    Task<List<Assignment>> GetByUserIdAsync(int userId);
    Task<Assignment?> GetByIdAsync(int id, int userId);
    Task<Assignment?> CreateAsync(int userId, CreateAssignmentRequest req);
    Task<Assignment?> UpdateAsync(int id, int userId, UpdateAssignmentRequest req);
    Task<bool> DeleteAsync(int id, int userId);
    Task<bool> SetDocumentAsync(int id, int userId, string? documentPath, string? documentText);
    Task<bool> SetRoadmapJsonAsync(int id, int userId, string? roadmapJson);
}

public class AssignmentService : IAssignmentService
{
    private readonly IDatabaseService _db;

    public AssignmentService(IDatabaseService db) => _db = db;

    public async Task<List<Assignment>> GetByUserIdAsync(int userId)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(
            "SELECT id, user_id, title, description, due_date, priority, status, document_path, document_text, roadmap_json, created_at, updated_at FROM assignments WHERE user_id = @u ORDER BY due_date", conn);
        cmd.Parameters.AddWithValue("@u", userId);
        var list = new List<Assignment>();
        using var r = (MySqlDataReader)await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add(ReadAssignment(r));
        return list;
    }

    public async Task<Assignment?> GetByIdAsync(int id, int userId)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(
            "SELECT id, user_id, title, description, due_date, priority, status, document_path, document_text, roadmap_json, created_at, updated_at FROM assignments WHERE id = @id AND user_id = @u", conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@u", userId);
        using var r = (MySqlDataReader)await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? ReadAssignment(r) : null;
    }

    public async Task<bool> SetDocumentAsync(int id, int userId, string? documentPath, string? documentText)
    {
        var existing = await GetByIdAsync(id, userId);
        if (existing == null) return false;
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand("UPDATE assignments SET document_path = @path, document_text = @text WHERE id = @id AND user_id = @u", conn);
        cmd.Parameters.AddWithValue("@path", (object?)documentPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@text", (object?)documentText ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@u", userId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> SetRoadmapJsonAsync(int id, int userId, string? roadmapJson)
    {
        var existing = await GetByIdAsync(id, userId);
        if (existing == null) return false;
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand("UPDATE assignments SET roadmap_json = @json WHERE id = @id AND user_id = @u", conn);
        cmd.Parameters.AddWithValue("@json", (object?)roadmapJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@u", userId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<Assignment?> CreateAsync(int userId, CreateAssignmentRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Title) || string.IsNullOrWhiteSpace(req.DueDate)) return null;
        if (!DateTime.TryParseExact(req.DueDate.Trim(), new[] { "yyyy-MM-dd", "yyyy-M-d" }, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var due)) return null;
        var priority = new[] { "low", "medium", "high" }.Contains(req.Priority?.ToLowerInvariant()) ? req.Priority!.ToLowerInvariant() : "medium";

        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(
            "INSERT INTO assignments (user_id, title, description, due_date, priority) VALUES (@u, @t, @d, @due, @p)", conn);
        cmd.Parameters.AddWithValue("@u", userId);
        cmd.Parameters.AddWithValue("@t", req.Title.Trim());
        cmd.Parameters.AddWithValue("@d", (object?)req.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@due", due.Date);
        cmd.Parameters.AddWithValue("@p", priority);
        await cmd.ExecuteNonQueryAsync();
        var id = (int)cmd.LastInsertedId;
        return await GetByIdAsync(id, userId);
    }

    public async Task<Assignment?> UpdateAsync(int id, int userId, UpdateAssignmentRequest req)
    {
        var existing = await GetByIdAsync(id, userId);
        if (existing == null || string.IsNullOrWhiteSpace(req.Title) || string.IsNullOrWhiteSpace(req.DueDate)) return null;
        if (!DateTime.TryParseExact(req.DueDate.Trim(), new[] { "yyyy-MM-dd", "yyyy-M-d" }, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var due)) return null;
        var priority = new[] { "low", "medium", "high" }.Contains(req.Priority?.ToLowerInvariant()) ? req.Priority!.ToLowerInvariant() : existing.Priority;
        var status = new[] { "pending", "in_progress", "completed" }.Contains(req.Status?.ToLowerInvariant()) ? req.Status!.ToLowerInvariant() : existing.Status;

        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(
            "UPDATE assignments SET title=@t, description=@d, due_date=@due, priority=@p, status=@s WHERE id=@id AND user_id=@u", conn);
        cmd.Parameters.AddWithValue("@t", req.Title.Trim());
        cmd.Parameters.AddWithValue("@d", (object?)req.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@due", due.Date);
        cmd.Parameters.AddWithValue("@p", priority);
        cmd.Parameters.AddWithValue("@s", status);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@u", userId);
        await cmd.ExecuteNonQueryAsync();
        return await GetByIdAsync(id, userId);
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand("DELETE FROM assignments WHERE id = @id AND user_id = @u", conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@u", userId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    private static Assignment ReadAssignment(MySqlDataReader r)
    {
        var a = new Assignment
        {
            Id = r.GetInt32(r.GetOrdinal("id")),
            UserId = r.GetInt32(r.GetOrdinal("user_id")),
            Title = r.GetString(r.GetOrdinal("title")),
            Description = r.IsDBNull(r.GetOrdinal("description")) ? null : r.GetString(r.GetOrdinal("description")),
            DueDate = r.GetDateTime(r.GetOrdinal("due_date")),
            Priority = r.GetString(r.GetOrdinal("priority")),
            Status = r.GetString(r.GetOrdinal("status")),
            CreatedAt = r.GetDateTime(r.GetOrdinal("created_at")),
            UpdatedAt = r.GetDateTime(r.GetOrdinal("updated_at"))
        };
        var pathOrd = r.GetOrdinal("document_path");
        var textOrd = r.GetOrdinal("document_text");
        if (!r.IsDBNull(pathOrd)) a.DocumentPath = r.GetString(pathOrd);
        if (!r.IsDBNull(textOrd)) a.DocumentText = r.GetString(textOrd);
        try { var jsonOrd = r.GetOrdinal("roadmap_json"); if (!r.IsDBNull(jsonOrd)) a.RoadmapJson = r.GetString(jsonOrd); } catch { /* column may not exist yet */ }
        return a;
    }
}
