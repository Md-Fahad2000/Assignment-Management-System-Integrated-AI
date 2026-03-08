using System.Data;
using AssignmentManagement.Api.Models;
using MySql.Data.MySqlClient;

namespace AssignmentManagement.Api.Services;

public interface IRoutineService
{
    Task<List<RoutineSlot>> GetByUserIdAsync(int userId);
    Task<List<RoutineSlot>> GetByUserAndDayAsync(int userId, int dayOfWeek);
    Task<RoutineSlot?> GetByIdAsync(int id, int userId);
    Task<RoutineSlot?> CreateAsync(int userId, CreateRoutineSlotRequest req);
    Task<RoutineSlot?> UpdateAsync(int id, int userId, UpdateRoutineSlotRequest req);
    Task<bool> DeleteAsync(int id, int userId);
}

public class RoutineService : IRoutineService
{
    private readonly IDatabaseService _db;

    public RoutineService(IDatabaseService db) => _db = db;

    public async Task<List<RoutineSlot>> GetByUserIdAsync(int userId)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(
            "SELECT id, user_id, day_of_week, start_time, end_time, title, description, created_at, updated_at FROM routine_slots WHERE user_id = @u ORDER BY day_of_week, start_time", conn);
        cmd.Parameters.AddWithValue("@u", userId);
        var list = new List<RoutineSlot>();
        using var r = (MySqlDataReader)await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(ReadSlot(r));
        return list;
    }

    public async Task<List<RoutineSlot>> GetByUserAndDayAsync(int userId, int dayOfWeek)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(
            "SELECT id, user_id, day_of_week, start_time, end_time, title, description, created_at, updated_at FROM routine_slots WHERE user_id = @u AND day_of_week = @d ORDER BY start_time", conn);
        cmd.Parameters.AddWithValue("@u", userId);
        cmd.Parameters.AddWithValue("@d", dayOfWeek);
        var list = new List<RoutineSlot>();
        using var r = (MySqlDataReader)await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(ReadSlot(r));
        return list;
    }

    public async Task<RoutineSlot?> GetByIdAsync(int id, int userId)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(
            "SELECT id, user_id, day_of_week, start_time, end_time, title, description, created_at, updated_at FROM routine_slots WHERE id = @id AND user_id = @u", conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@u", userId);
        using var r = (MySqlDataReader)await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? ReadSlot(r) : null;
    }

    public async Task<RoutineSlot?> CreateAsync(int userId, CreateRoutineSlotRequest req)
    {
        if (req.DayOfWeek < 0 || req.DayOfWeek > 6 || string.IsNullOrWhiteSpace(req.Title)) return null;
        if (!TimeSpan.TryParse(req.StartTime, out var start) || !TimeSpan.TryParse(req.EndTime, out var end)) return null;

        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(
            "INSERT INTO routine_slots (user_id, day_of_week, start_time, end_time, title, description) VALUES (@u, @d, @s, @e, @t, @desc)", conn);
        cmd.Parameters.AddWithValue("@u", userId);
        cmd.Parameters.AddWithValue("@d", req.DayOfWeek);
        cmd.Parameters.AddWithValue("@s", start);
        cmd.Parameters.AddWithValue("@e", end);
        cmd.Parameters.AddWithValue("@t", req.Title.Trim());
        cmd.Parameters.AddWithValue("@desc", (object?)req.Description ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
        var id = (int)cmd.LastInsertedId;
        return await GetByIdAsync(id, userId);
    }

    public async Task<RoutineSlot?> UpdateAsync(int id, int userId, UpdateRoutineSlotRequest req)
    {
        var existing = await GetByIdAsync(id, userId);
        if (existing == null || req.DayOfWeek < 0 || req.DayOfWeek > 6 || string.IsNullOrWhiteSpace(req.Title)) return null;
        if (!TimeSpan.TryParse(req.StartTime, out var start) || !TimeSpan.TryParse(req.EndTime, out var end)) return null;

        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(
            "UPDATE routine_slots SET day_of_week=@d, start_time=@s, end_time=@e, title=@t, description=@desc WHERE id=@id AND user_id=@u", conn);
        cmd.Parameters.AddWithValue("@d", req.DayOfWeek);
        cmd.Parameters.AddWithValue("@s", start);
        cmd.Parameters.AddWithValue("@e", end);
        cmd.Parameters.AddWithValue("@t", req.Title.Trim());
        cmd.Parameters.AddWithValue("@desc", (object?)req.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@u", userId);
        await cmd.ExecuteNonQueryAsync();
        return await GetByIdAsync(id, userId);
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand("DELETE FROM routine_slots WHERE id = @id AND user_id = @u", conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@u", userId);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    private static RoutineSlot ReadSlot(MySqlDataReader r) => new()
    {
        Id = r.GetInt32("id"),
        UserId = r.GetInt32("user_id"),
        DayOfWeek = r.GetInt32("day_of_week"),
        StartTime = GetTimeSpan(r, "start_time"),
        EndTime = GetTimeSpan(r, "end_time"),
        Title = r.GetString("title"),
        Description = r.IsDBNull(r.GetOrdinal("description")) ? null : r.GetString("description"),
        CreatedAt = r.GetDateTime("created_at"),
        UpdatedAt = r.GetDateTime("updated_at")
    };

    private static TimeSpan GetTimeSpan(MySqlDataReader r, string col)
    {
        var val = r.GetValue(r.GetOrdinal(col));
        if (val is TimeSpan ts) return ts;
        if (val is DateTime dt) return dt.TimeOfDay;
        if (val is string str && TimeSpan.TryParse(str, out var parsed)) return parsed;
        return TimeSpan.Zero;
    }
}
