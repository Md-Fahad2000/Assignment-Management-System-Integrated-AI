using System.Text.Json;
using MySql.Data.MySqlClient;

namespace AssignmentManagement.Api.Services;

public interface IStressService
{
    Task LogAsync(int userId, DateTime date, int stressLevel, string? note = null);
    Task<StressSummaryDto> GetSummaryAsync(int userId);
}

public class StressSummaryDto
{
    public double? PreviousWeek { get; set; }
    public double? CurrentWeek { get; set; }
    public double? FuturePredicted { get; set; }
    public string? Message { get; set; }
}

public class StressService : IStressService
{
    private readonly IDatabaseService _db;
    private readonly IAssignmentService _assignments;
    private readonly IRoutineService _routine;
    private readonly IAssignmentExtraService _extra;

    public StressService(IDatabaseService db, IAssignmentService assignments, IRoutineService routine, IAssignmentExtraService extra)
    {
        _db = db;
        _assignments = assignments;
        _routine = routine;
        _extra = extra;
    }

    public async Task LogAsync(int userId, DateTime date, int stressLevel, string? note = null)
    {
        if (stressLevel < 1 || stressLevel > 5) return;
        var d = date.Date;
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(
            @"INSERT INTO stress_logs (user_id, log_date, stress_level, note)
              VALUES (@uid, @d, @level, @note)
              ON DUPLICATE KEY UPDATE stress_level = @level, note = @note", conn);
        cmd.Parameters.AddWithValue("@uid", userId);
        cmd.Parameters.AddWithValue("@d", d.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@level", stressLevel);
        cmd.Parameters.AddWithValue("@note", note ?? "");
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<StressSummaryDto> GetSummaryAsync(int userId)
    {
        var result = new StressSummaryDto();
        var assignments = await _assignments.GetByUserIdAsync(userId);
        var routineSlots = await _routine.GetByUserIdAsync(userId);
        var routineSummary = routineSlots.Count == 0
            ? "No fixed routine."
            : string.Join("; ", routineSlots
                .GroupBy(r => r.DayOfWeek)
                .OrderBy(g => g.Key)
                .Select(g => new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" }[g.Key] + ": " + string.Join(", ", g.Select(s => s.StartTime.ToString(@"hh\:mm") + "-" + s.EndTime.ToString(@"hh\:mm") + " " + s.Title))));

        try
        {
            var json = await _extra.GetStressEstimateAsync(assignments, routineSummary);
            if (!string.IsNullOrWhiteSpace(json))
            {
                json = json.Trim();
                if (json.StartsWith("```")) json = json.Replace("```json", "").Replace("```", "").Trim();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("previous_week", out var pw) && pw.TryGetDouble(out var pv)) result.PreviousWeek = Math.Min(5, Math.Max(1, Math.Round(pv, 1)));
                if (root.TryGetProperty("current_week", out var cw) && cw.TryGetDouble(out var cv)) result.CurrentWeek = Math.Min(5, Math.Max(1, Math.Round(cv, 1)));
                if (root.TryGetProperty("future_predicted", out var fw) && fw.TryGetDouble(out var fv)) result.FuturePredicted = Math.Min(5, Math.Max(1, Math.Round(fv, 1)));
                if (root.TryGetProperty("message", out var msg)) result.Message = msg.GetString();
                if (result.PreviousWeek.HasValue || result.CurrentWeek.HasValue || result.FuturePredicted.HasValue)
                    return result;
            }
        }
        catch
        {
            
        }

        
        var today = DateTime.Today;
        var pending = assignments.Where(a => (a.Status ?? "").ToLowerInvariant() != "completed").ToList();
        var overdue = pending.Count(a => a.DueDate.Date < today);
        var dueIn7 = pending.Count(a => { var d = (a.DueDate.Date - today).TotalDays; return d >= 0 && d <= 7; });
        var dueIn14 = pending.Count(a => { var d = (a.DueDate.Date - today).TotalDays; return d >= 0 && d <= 14; });

        double workload = 2.5;
        if (overdue > 0) workload += 0.5 * Math.Min(overdue, 4);
        if (dueIn7 > 0) workload += 0.3 * Math.Min(dueIn7, 5);
        if (dueIn14 > dueIn7) workload += 0.1 * Math.Min(dueIn14 - dueIn7, 5);
        var value = Math.Min(5, Math.Max(1, Math.Round(workload, 1)));

        result.PreviousWeek = value;
        result.CurrentWeek = value;
        result.FuturePredicted = value;
        if (overdue > 0 || dueIn7 >= 3)
            result.Message = "Upcoming deadlines may increase stress. Consider breaking tasks into smaller steps.";
        return result;
    }
}
