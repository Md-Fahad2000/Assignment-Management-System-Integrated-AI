using AssignmentManagement.Api.Models;
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

/// <summary>
/// Mental stress from routine + assignments: each assignment assumes 2h/day from free time.
/// Count bands: 1 → Low, 2 → Mild, 3 → Moderate, 4+ → Extreme. Routine load can raise the level.
/// </summary>
public class StressService : IStressService
{
    private readonly IDatabaseService _db;
    private readonly IAssignmentService _assignments;
    private readonly IRoutineService _routine;

    /// <summary>Hours per assignment per day (user requirement).</summary>
    private const double HoursPerAssignmentPerDay = 2.0;

    public StressService(IDatabaseService db, IAssignmentService assignments, IRoutineService routine)
    {
        _db = db;
        _assignments = assignments;
        _routine = routine;
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
        var assignments = await _assignments.GetByUserIdAsync(userId);
        var routineSlots = await _routine.GetByUserIdAsync(userId);

        var freeHoursPerDay = ComputeFreeHoursPerDay(routineSlots);

        var today = DateTime.Today;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var prevWeekStart = weekStart.AddDays(-7);
        var nextWeekStart = weekStart.AddDays(7);

        var pending = assignments.Where(a => !string.Equals(a.Status, "completed", StringComparison.OrdinalIgnoreCase)).ToList();

        int countPendingDueInRange(DateTime start, DateTime endInclusive) =>
            pending.Count(a => a.DueDate.Date >= start && a.DueDate.Date <= endInclusive);

        var nPrev = countPendingDueInRange(prevWeekStart, prevWeekStart.AddDays(6));
        var nCurr = countPendingDueInRange(weekStart, weekStart.AddDays(6));
        var nNext = countPendingDueInRange(nextWeekStart, nextWeekStart.AddDays(6));

        var result = new StressSummaryDto
        {
            PreviousWeek = ComputeStressLevel(nPrev, freeHoursPerDay),
            CurrentWeek = ComputeStressLevel(nCurr, freeHoursPerDay),
            FuturePredicted = ComputeStressLevel(nNext, freeHoursPerDay),
            Message = BuildMessage(freeHoursPerDay, nCurr)
        };

        return result;
    }

    /// <summary>Average routine busy hours per day (sum of weekly slot durations / 7).</summary>
    private static double ComputeAverageDailyBusyHours(IReadOnlyList<RoutineSlot> slots)
    {
        if (slots.Count == 0) return 0;

        double[] perDay = new double[7];
        foreach (var s in slots)
        {
            var d = s.DayOfWeek;
            if (d < 0 || d > 6) continue;
            var span = Math.Max(0, (s.EndTime - s.StartTime).TotalHours);
            perDay[d] += span;
        }

        var totalWeek = perDay.Sum();
        return totalWeek / 7.0;
    }

    /// <summary>Free hours per day = 24 − average busy (from routine).</summary>
    private static double ComputeFreeHoursPerDay(IReadOnlyList<RoutineSlot> slots)
    {
        var busy = ComputeAverageDailyBusyHours(slots);
        if (busy >= 24) return 0.5;
        return Math.Max(0.5, 24.0 - busy);
    }

    /// <summary>
    /// Base: 0–1 → Low, 2 → Mild, 3 → High, 4+ → Extreme.
    /// Then adjust if daily required (2h × n) exceeds free time proportionally.
    /// </summary>
    private static double ComputeStressLevel(int assignmentCount, double freeHoursPerDay)
    {
        var n = assignmentCount;
        var baseLevel = BaseStressFromAssignmentCount(n);
        var requiredDaily = n * HoursPerAssignmentPerDay;
        var ratio = freeHoursPerDay > 0 ? requiredDaily / freeHoursPerDay : 10.0;

        var level = baseLevel;
        if (ratio > 1.0)
            level = Math.Min(5, level + 2);
        else if (ratio > 0.85)
            level = Math.Min(5, level + 1);
        else if (ratio > 0.65 && n >= 2)
            level = Math.Min(5, level + 1);

        return Math.Round((double)Math.Min(5, Math.Max(1, level)), 1);
    }

    /// <summary>User mapping: 1 → Low, 2 → Mild, 3 → High, 4+ → Extreme.</summary>
    private static int BaseStressFromAssignmentCount(int n)
    {
        if (n <= 0) return 1;
        if (n == 1) return 1;
        if (n == 2) return 2;
        if (n == 3) return 4;
        return 5;
    }

    private static string? BuildMessage(double freeHoursPerDay, int assignmentsThisWeek)
    {
        var req = assignmentsThisWeek * HoursPerAssignmentPerDay;
        var ratio = freeHoursPerDay > 0 ? req / freeHoursPerDay : 0;
        if (ratio > 1.0)
            return $"Your routine leaves about {freeHoursPerDay:F1} h/day free; {assignmentsThisWeek} assignment(s) need ~{req:F1} h/day combined — that exceeds typical free time. Consider lightening the week or rescheduling.";
        if (assignmentsThisWeek >= 4)
            return "Four or more assignments due this week — stress level is Extreme. Plan breaks and prioritize deadlines.";
        return $"Stress is estimated from workload (2 h/day per assignment) and your routine (~{freeHoursPerDay:F1} h/day free on average).";
    }
}
