using AssignmentManagement.Api.Models;
using MySql.Data.MySqlClient;

namespace AssignmentManagement.Api.Services;

public interface IAssignmentChatService
{
    Task<List<AssignmentAssistantMessage>> GetMessagesAsync(int assignmentId, int userId);
    Task<int> AddMessageAsync(int assignmentId, int userId, string role, string content);
    Task<bool> SetMessageRatingAsync(int assignmentId, int userId, int messageId, int rating);
}

public class AssignmentChatService : IAssignmentChatService
{
    private readonly IDatabaseService _db;

    public AssignmentChatService(IDatabaseService db) => _db = db;

    public async Task<List<AssignmentAssistantMessage>> GetMessagesAsync(int assignmentId, int userId)
    {
        var list = new List<AssignmentAssistantMessage>();
        try
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            using var cmd = new MySqlCommand(
                "SELECT id, role, content FROM assignment_chat_messages WHERE assignment_id = @aid AND user_id = @uid ORDER BY created_at ASC", conn);
            cmd.Parameters.AddWithValue("@aid", assignmentId);
            cmd.Parameters.AddWithValue("@uid", userId);
            using var r = (MySqlDataReader)await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var msg = new AssignmentAssistantMessage { Role = r.GetString(1), Content = r.GetString(2) };
                try { msg.Id = r.GetInt32(0); } catch { }
                list.Add(msg);
            }
        }
        catch (MySqlException)
        {
            
        }
        return list;
    }

    public async Task<int> AddMessageAsync(int assignmentId, int userId, string role, string content)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(
            "INSERT INTO assignment_chat_messages (assignment_id, user_id, role, content) VALUES (@aid, @uid, @role, @content)", conn);
        cmd.Parameters.AddWithValue("@aid", assignmentId);
        cmd.Parameters.AddWithValue("@uid", userId);
        cmd.Parameters.AddWithValue("@role", role);
        cmd.Parameters.AddWithValue("@content", content ?? "");
        await cmd.ExecuteNonQueryAsync();
        return (int)cmd.LastInsertedId;
    }

    public async Task<bool> SetMessageRatingAsync(int assignmentId, int userId, int messageId, int rating)
    {
        if (rating != 1 && rating != -1) return false;
        try
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            using var cmd = new MySqlCommand(
                "UPDATE assignment_chat_messages SET rating = @r WHERE id = @mid AND assignment_id = @aid AND user_id = @uid", conn);
            cmd.Parameters.AddWithValue("@r", rating);
            cmd.Parameters.AddWithValue("@mid", messageId);
            cmd.Parameters.AddWithValue("@aid", assignmentId);
            cmd.Parameters.AddWithValue("@uid", userId);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
        catch (MySqlException) { return false; }
    }
}
