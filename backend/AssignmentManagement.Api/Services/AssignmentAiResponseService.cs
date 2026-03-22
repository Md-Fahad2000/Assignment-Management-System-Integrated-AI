using MySql.Data.MySqlClient;

namespace AssignmentManagement.Api.Services;

public interface IAssignmentAiResponseService
{
    Task SetAsync(int assignmentId, int userId, string responseType, string responseValue);
    Task<Dictionary<string, string>> GetAllAsync(int assignmentId, int userId);
}

public class AssignmentAiResponseService : IAssignmentAiResponseService
{
    private readonly IDatabaseService _db;

    public AssignmentAiResponseService(IDatabaseService db) => _db = db;

    public async Task SetAsync(int assignmentId, int userId, string responseType, string responseValue)
    {
        if (string.IsNullOrWhiteSpace(responseType)) return;
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(
            @"INSERT INTO assignment_ai_responses (assignment_id, user_id, response_type, response_value)
              VALUES (@aid, @uid, @type, @value)
              ON DUPLICATE KEY UPDATE response_value = @value, updated_at = CURRENT_TIMESTAMP", conn);
        cmd.Parameters.AddWithValue("@aid", assignmentId);
        cmd.Parameters.AddWithValue("@uid", userId);
        cmd.Parameters.AddWithValue("@type", responseType);
        cmd.Parameters.AddWithValue("@value", responseValue ?? "");
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<Dictionary<string, string>> GetAllAsync(int assignmentId, int userId)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            using var cmd = new MySqlCommand(
                "SELECT response_type, response_value FROM assignment_ai_responses WHERE assignment_id = @aid AND user_id = @uid", conn);
            cmd.Parameters.AddWithValue("@aid", assignmentId);
            cmd.Parameters.AddWithValue("@uid", userId);
            using var r = (MySqlDataReader)await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                result[r.GetString(0)] = r.GetString(1);
        }
        catch (MySqlException) {  }
        return result;
    }
}
