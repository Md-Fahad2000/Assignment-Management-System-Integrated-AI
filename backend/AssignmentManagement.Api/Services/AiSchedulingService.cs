using System.Text;
using System.Text.RegularExpressions;
using AssignmentManagement.Api.Models;
using MySql.Data.MySqlClient;

namespace AssignmentManagement.Api.Services;

public class RoadmapContext
{
    public string AssignmentTitle { get; set; } = "";
    public string? DueDate { get; set; }
    public string? AssignmentDescription { get; set; }
    public string? DocumentText { get; set; }
    public string RoutineSummary { get; set; } = "";
}

public interface IAiSchedulingService
{
    Task<List<AiScheduleSlot>> GenerateScheduleAsync(int userId, DateTime fromDate, DateTime toDate);
    Task<List<AiScheduleSlot>> GetScheduleAsync(int userId, DateTime fromDate, DateTime toDate);
    Task<List<RoadmapStep>> GenerateRoadmapAsync(int assignmentId, int userId);
    Task<List<RoadmapStep>> GetRoadmapAsync(int assignmentId, int userId);
    Task<RoadmapContext?> GetRoadmapContextAsync(int assignmentId, int userId);
    /// <summary>Stream AI roadmap from Gemini and save when complete. onChunk is called for each live text chunk.</summary>
    Task<List<RoadmapStep>> StreamGenerateRoadmapAsync(int assignmentId, int userId, Func<string, Task> onChunk, CancellationToken cancellationToken = default);
    double GetPriorityScore(DateTime dueDate, string priority);
}

/// <summary>
/// </summary>
public class AiSchedulingService : IAiSchedulingService
{
    private readonly IDatabaseService _db;
    private readonly IOpenAiRoadmapService? _openAi;
    private readonly IGeminiRoadmapService? _gemini;
    private const int DefaultSlotMinutes = 120; 
    private static readonly string[] ActionableStepTemplates = new[]
    {
        "Read the assignment and list all requirements",
        "Research and gather materials / sources",
        "Outline and plan structure",
        "Draft first version or first section",
        "Draft remaining sections",
        "Review and revise content",
        "Proofread and fix errors",
        "Final check and submit"
    };
    /// <summary>.</summary>
    private static readonly string[] ActionableStepDetails = new[]
    {
        "Open the assignment file or PDF. Read every question and instruction. Note word limits, due dates, and marking criteria. List all deliverables on a page.",
        "Search books, articles, or the web for 3–5 reliable sources. Save links and key points. Note citation format (e.g. APA).",
        "Create headings and sub-points for your answer. Decide what goes in intro, body, and conclusion. Allocate word count per section.",
        "Write the first section or first draft without worrying about perfection. Follow your outline. Use clear sentences.",
        "Complete the remaining sections or paragraphs. Keep tone and style consistent. Support claims with evidence.",
        "Re-read the full draft. Improve clarity, flow, and logic. Cut or add where needed. Check it answers the question.",
        "Check spelling, grammar, and punctuation. Ensure references and formatting match the required style.",
        "Do a final read. Submit before the deadline. Keep a copy for your records."
    };
    private static readonly string[] DefaultRoadmapTemplates = new[] { "Research and gather materials", "Outline and plan structure", "Draft first version", "Review and revise", "Final proofread and submit" };
    private static readonly TimeSpan[] DefaultStepTimes = new[]
    {
        TimeSpan.FromHours(9), TimeSpan.FromHours(14), TimeSpan.FromHours(16), TimeSpan.FromHours(18)
    };

    public AiSchedulingService(IDatabaseService db, IOpenAiRoadmapService? openAi = null, IGeminiRoadmapService? gemini = null)
    {
        _db = db;
        _openAi = openAi;
        _gemini = gemini;
    }

    /// <summary>Priority score: higher = more urgent. Formula: base from deadline + priority multiplier.</summary>
    public double GetPriorityScore(DateTime dueDate, string priority)
    {
        var daysLeft = (dueDate.Date - DateTime.Today).TotalDays;
        if (daysLeft < 0) daysLeft = 0;
        var urgency = daysLeft <= 1 ? 100 : daysLeft <= 3 ? 80 : daysLeft <= 7 ? 60 : daysLeft <= 14 ? 40 : 20;
        var priorityMultiplier = priority?.ToLowerInvariant() switch { "high" => 1.5, "medium" => 1.0, _ => 0.7 };
        return urgency * priorityMultiplier;
    }

    /// <summary>.</summary>
    public async Task<List<AiScheduleSlot>> GenerateScheduleAsync(int userId, DateTime fromDate, DateTime toDate)
    {
        var assignments = await GetAssignmentsSortedByPriorityAsync(userId);
        var routineSlots = await GetRoutineSlotsAsync(userId);
        var slots = new List<AiScheduleSlot>();
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        await ClearAiSchedulesForUserAsync(conn, userId, fromDate, toDate);

        foreach (var a in assignments.Where(x => x.Status != "completed"))
        {
            var due = a.DueDate.Date;
            if (due < fromDate) continue;
            var studyMinutes = Math.Max(60, DefaultSlotMinutes);
            var start = fromDate.Date;
            while (start <= toDate.Date && start <= due)
            {
                for (var dayOfWeek = 0; dayOfWeek <= 6; dayOfWeek++)
                {
                    if ((int)start.DayOfWeek != dayOfWeek) continue;
                    var freeWindows = GetFreeWindows(start, routineSlots.Where(r => r.DayOfWeek == dayOfWeek).ToList());
                    foreach (var (slotStart, slotEnd) in freeWindows)
                    {
                        if (studyMinutes <= 0) break;
                        var duration = Math.Min(studyMinutes, (int)(slotEnd - slotStart).TotalMinutes);
                        if (duration < 30) continue;
                        var endTime = slotStart.AddMinutes(duration);
                        await InsertAiScheduleAsync(conn, userId, a.Id, start, slotStart.TimeOfDay, endTime.TimeOfDay, a.Title);
                        slots.Add(new AiScheduleSlot
                        {
                            AssignmentId = a.Id,
                            SlotDate = start,
                            StartTime = slotStart.TimeOfDay,
                            EndTime = endTime.TimeOfDay,
                            AssignmentTitle = a.Title
                        });
                        studyMinutes -= duration;
                        if (studyMinutes <= 0) break;
                    }
                    if (studyMinutes <= 0) break;
                }
                start = start.AddDays(1);
                if (studyMinutes <= 0) break;
            }
        }

        return slots;
    }

    public async Task<List<AiScheduleSlot>> GetScheduleAsync(int userId, DateTime fromDate, DateTime toDate)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(
            "SELECT s.id, s.assignment_id, s.slot_date, s.start_time, s.end_time, s.notes, a.title AS assignment_title FROM ai_schedules s LEFT JOIN assignments a ON s.assignment_id = a.id WHERE s.user_id = @u AND s.slot_date >= @f AND s.slot_date <= @t ORDER BY s.slot_date, s.start_time", conn);
        cmd.Parameters.AddWithValue("@u", userId);
        cmd.Parameters.AddWithValue("@f", fromDate.Date);
        cmd.Parameters.AddWithValue("@t", toDate.Date);
        var list = new List<AiScheduleSlot>();
        using var r = (MySqlDataReader)await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add(new AiScheduleSlot
            {
                Id = r.GetInt32(r.GetOrdinal("id")),
                AssignmentId = r.IsDBNull(r.GetOrdinal("assignment_id")) ? null : r.GetInt32(r.GetOrdinal("assignment_id")),
                SlotDate = r.GetDateTime(r.GetOrdinal("slot_date")),
                StartTime = GetTimeSpan(r, "start_time"),
                EndTime = GetTimeSpan(r, "end_time"),
                Notes = r.IsDBNull(r.GetOrdinal("notes")) ? null : r.GetString(r.GetOrdinal("notes")),
                AssignmentTitle = r.IsDBNull(r.GetOrdinal("assignment_title")) ? null : r.GetString(r.GetOrdinal("assignment_title"))
            });
        return list;
    }

    /// <summary>.</summary>
    public async Task<List<RoadmapStep>> GenerateRoadmapAsync(int assignmentId, int userId)
    {
        var assignment = await GetAssignmentByIdAsync(assignmentId, userId);
        if (assignment == null) return new List<RoadmapStep>();

        var due = assignment.DueDate.Date;
        var today = DateTime.Today;
        if (due < today) due = today;
        var routineSlots = await GetRoutineSlotsAsync(userId);
        var routineSummary = BuildRoutineSummary(routineSlots);
        var freeSlotsForRoadmap = GetFreeSlotsForRoadmap(today, due, routineSlots);

        List<RoadmapStepDto>? aiSteps = null;
        if (_openAi != null)
            aiSteps = await _openAi.GetRoadmapFromAiAsync(assignment.Title, assignment.DocumentText, due, routineSummary, assignment.Description);
        if ((aiSteps == null || aiSteps.Count == 0) && _gemini != null)
            aiSteps = await _gemini.GetRoadmapFromAiAsync(assignment.Title, assignment.DocumentText, due, routineSummary, assignment.Description);

        if (aiSteps == null || aiSteps.Count == 0)
            return new List<RoadmapStep>();

        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        await DeleteRoadmapStepsAsync(conn, assignmentId);
        var steps = new List<RoadmapStep>();
        if (freeSlotsForRoadmap.Count == 0) freeSlotsForRoadmap.Add((due, TimeSpan.FromHours(9), TimeSpan.FromHours(11)));
        for (var i = 0; i < aiSteps.Count; i++)
        {
            var s = aiSteps[i];
            var (slotDate, slotStart, slotEnd) = freeSlotsForRoadmap[i % freeSlotsForRoadmap.Count];
            if (slotDate > due) slotDate = due;
            var title = (s.step_title?.Trim() ?? $"Step {i + 1}").Length > 500 ? s.step_title!.Trim().Substring(0, 497) + "..." : (s.step_title?.Trim() ?? $"Step {i + 1}");
            var detail = (s.step_detail?.Trim() ?? "").Length > 2000 ? s.step_detail!.Trim().Substring(0, 1997) + "..." : (s.step_detail?.Trim());
            var id = await InsertRoadmapStepAsync(conn, assignmentId, i + 1, title, slotDate, slotStart, detail, slotEnd);
            steps.Add(new RoadmapStep { Id = id, AssignmentId = assignmentId, StepOrder = i + 1, StepTitle = title, StepDetail = detail, SuggestedDate = slotDate, SuggestedTime = slotStart, SuggestedEndTime = slotEnd, Completed = false });
        }
        return steps;
    }

    private static string BuildRoutineSummary(List<RoutineSlot> routineSlots)
    {
        if (routineSlots.Count == 0) return "No fixed routine.";
        var days = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
        var byDay = routineSlots.GroupBy(r => r.DayOfWeek).OrderBy(g => g.Key);
        return string.Join("; ", byDay.Select(g => days[g.Key] + ": " + string.Join(", ", g.Select(s => s.StartTime.ToString(@"hh\:mm") + "-" + s.EndTime.ToString(@"hh\:mm") + " " + s.Title))));
    }

    /// <summary>.</summary>
    private static List<(DateTime date, TimeSpan time)> GetAvailableDateTimeSlots(DateTime from, DateTime to, List<RoutineSlot> routineSlots)
    {
        var slots = new List<(DateTime, TimeSpan)>();
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            var dayRoutine = routineSlots.Where(r => r.DayOfWeek == (int)d.DayOfWeek).ToList();
            var free = GetFreeWindows(d, dayRoutine);
            if (free.Count > 0)
            {
                foreach (var (start, _) in free.Take(3))
                    slots.Add((d, start.TimeOfDay));
            }
            else
            {
                foreach (var t in DefaultStepTimes)
                    slots.Add((d, t));
            }
        }
        return slots.OrderBy(x => x.Item1).ThenBy(x => x.Item2).ToList();
    }

    /// <summary>.</summary>
    private static List<(DateTime date, TimeSpan start, TimeSpan end)> GetFreeSlotsForRoadmap(DateTime from, DateTime to, List<RoutineSlot> routineSlots, int maxSlots = 50)
    {
        var list = new List<(DateTime, TimeSpan, TimeSpan)>();
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            var dayRoutine = routineSlots.Where(r => r.DayOfWeek == (int)d.DayOfWeek).ToList();
            var free = GetFreeWindows(d, dayRoutine);
            if (free.Count > 0)
            {
                foreach (var (start, end) in free)
                {
                    var durationHours = (end - start).TotalHours;
                    var numSlots = Math.Max(1, Math.Min(6, (int)Math.Ceiling(durationHours)));
                    for (var i = 0; i < numSlots && list.Count < maxSlots; i++)
                    {
                        var slotStart = start.AddHours(i * (durationHours / numSlots));
                        var slotEnd = start.AddHours((i + 1) * (durationHours / numSlots));
                        if (slotEnd <= end)
                            list.Add((d, slotStart.TimeOfDay, slotEnd.TimeOfDay));
                    }
                }
            }
            else
            {
                foreach (var t in DefaultStepTimes)
                    list.Add((d, t, t + TimeSpan.FromHours(2)));
            }
        }
        return list.OrderBy(x => x.Item1).ThenBy(x => x.Item2).Take(maxSlots).ToList();
    }

    /// <summary>.</summary>
    private static List<string> SplitDocumentIntoSteps(string documentText)
    {
        var text = documentText.Trim();
        if (string.IsNullOrEmpty(text)) return new List<string>();

        var parts = new List<string>();
        var sectionPattern = new Regex(@"(?m)^\s*(?:Question\s*\d+|Part\s+[A-Za-z\d]+|Section\s+\d+|\d+[.)]\s*[A-Z]|[A-Z][.)]\s*)\s*", RegexOptions.IgnoreCase);
        var splits = sectionPattern.Split(text).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
        if (splits.Count >= 2)
        {
            foreach (var s in splits.Take(15))
                parts.Add(s.Length > 500 ? s.Substring(0, 497) + "..." : s);
            return parts;
        }
        var paragraphs = Regex.Split(text, @"\n\s*\n").Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p.Trim()).ToList();
        if (paragraphs.Count >= 2)
        {
            var chunkSize = Math.Max(1, (paragraphs.Count + 9) / 10);
            for (var i = 0; i < paragraphs.Count; i += chunkSize)
            {
                var chunk = string.Join(" ", paragraphs.Skip(i).Take(chunkSize));
                parts.Add(chunk.Length > 500 ? chunk.Substring(0, 497) + "..." : chunk);
            }
            return parts.Take(15).ToList();
        }
        var lines = text.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        if (lines.Count >= 2)
        {
            var stepLines = Math.Max(1, (lines.Count + 9) / 10);
            for (var i = 0; i < lines.Count; i += stepLines)
                parts.Add(string.Join(" ", lines.Skip(i).Take(stepLines)).Trim());
            return parts.Take(15).ToList();
        }
        parts.Add(text.Length > 500 ? text.Substring(0, 497) + "..." : text);
        return parts;
    }

    /// <summary>.</summary>
    private static List<DateTime> GetAvailableDatesFromNowToDeadline(DateTime from, DateTime to, List<RoutineSlot> routineSlots)
    {
        var dates = new List<DateTime>();
        for (var d = from; d <= to; d = d.AddDays(1))
            dates.Add(d);
        if (dates.Count == 0) return dates;
        var busyByDay = routineSlots.GroupBy(r => r.DayOfWeek).ToDictionary(g => g.Key, g => g.Count());
        var dayOfWeek = (int)from.DayOfWeek;
        return dates.OrderBy(d => busyByDay.GetValueOrDefault((int)d.DayOfWeek, 0)).ToList();
    }

    public async Task<List<RoadmapStep>> GetRoadmapAsync(int assignmentId, int userId)
    {
        var assignment = await GetAssignmentByIdAsync(assignmentId, userId);
        if (assignment == null) return new List<RoadmapStep>();
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        var list = new List<RoadmapStep>();
        try
        {
            using var cmd = new MySqlCommand(
                "SELECT id, assignment_id, step_order, step_title, step_detail, suggested_date, suggested_time, suggested_end_time, completed FROM assignment_roadmap_steps WHERE assignment_id = @a ORDER BY step_order", conn);
            cmd.Parameters.AddWithValue("@a", assignmentId);
            using var r = (MySqlDataReader)await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
                list.Add(ReadRoadmapStep(r, full: true));
        }
        catch (MySqlException)
        {
            try
            {
                using var cmd = new MySqlCommand(
                    "SELECT id, assignment_id, step_order, step_title, suggested_date, suggested_time, completed FROM assignment_roadmap_steps WHERE assignment_id = @a ORDER BY step_order", conn);
                cmd.Parameters.AddWithValue("@a", assignmentId);
                using var r = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    list.Add(ReadRoadmapStep(r, includeTime: true));
            }
            catch (MySqlException)
            {
                using var cmd = new MySqlCommand(
                    "SELECT id, assignment_id, step_order, step_title, suggested_date, completed FROM assignment_roadmap_steps WHERE assignment_id = @a ORDER BY step_order", conn);
                cmd.Parameters.AddWithValue("@a", assignmentId);
                using var r = (MySqlDataReader)await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    list.Add(ReadRoadmapStep(r, includeTime: false));
            }
        }
        return list;
    }

    /// <summary>.</summary>
    public async Task<List<RoadmapStep>> StreamGenerateRoadmapAsync(int assignmentId, int userId, Func<string, Task> onChunk, CancellationToken cancellationToken = default)
    {
        var assignment = await GetAssignmentByIdAsync(assignmentId, userId);
        if (assignment == null) return new List<RoadmapStep>();
        var due = assignment.DueDate.Date;
        var today = DateTime.Today;
        if (due < today) due = today;
        var routineSlots = await GetRoutineSlotsAsync(userId);
        var routineSummary = BuildRoutineSummary(routineSlots);

        if (_gemini == null) return new List<RoadmapStep>();

        var fullText = new StringBuilder();
        await foreach (var chunk in _gemini.StreamRoadmapFromGeminiAsync(assignment.Title, assignment.DocumentText, due, routineSummary, assignment.Description, cancellationToken))
        {
            fullText.Append(chunk);
            await onChunk(chunk);
        }

        var aiSteps = GeminiRoadmapService.ParseStepsFromJson(fullText.ToString());
        if (aiSteps == null || aiSteps.Count == 0)
            return new List<RoadmapStep>();

        var freeSlotsForRoadmap = GetFreeSlotsForRoadmap(today, due, routineSlots);
        if (freeSlotsForRoadmap.Count == 0) freeSlotsForRoadmap.Add((due, TimeSpan.FromHours(9), TimeSpan.FromHours(11)));

        using var conn = _db.GetConnection();
        await conn.OpenAsync(cancellationToken);
        await DeleteRoadmapStepsAsync(conn, assignmentId);
        var steps = new List<RoadmapStep>();
        for (var i = 0; i < aiSteps.Count; i++)
        {
            var s = aiSteps[i];
            var (slotDate, slotStart, slotEnd) = freeSlotsForRoadmap[i % freeSlotsForRoadmap.Count];
            if (slotDate > due) slotDate = due;
            var title = (s.step_title?.Trim() ?? $"Step {i + 1}").Length > 500 ? s.step_title!.Trim().Substring(0, 497) + "..." : (s.step_title?.Trim() ?? $"Step {i + 1}");
            var detail = (s.step_detail?.Trim() ?? "").Length > 2000 ? s.step_detail!.Trim().Substring(0, 1997) + "..." : (s.step_detail?.Trim());
            var id = await InsertRoadmapStepAsync(conn, assignmentId, i + 1, title, slotDate, slotStart, detail, slotEnd);
            steps.Add(new RoadmapStep { Id = id, AssignmentId = assignmentId, StepOrder = i + 1, StepTitle = title, StepDetail = detail, SuggestedDate = slotDate, SuggestedTime = slotStart, SuggestedEndTime = slotEnd, Completed = false });
        }
        return steps;
    }

    /// <summary>.</summary>
    public async Task<RoadmapContext?> GetRoadmapContextAsync(int assignmentId, int userId)
    {
        var assignment = await GetAssignmentByIdAsync(assignmentId, userId);
        if (assignment == null) return null;
        var routineSlots = await GetRoutineSlotsAsync(userId);
        var routineSummary = BuildRoutineSummary(routineSlots);
        var docText = assignment.DocumentText;
        if (!string.IsNullOrEmpty(docText) && docText.Length > 4000)
            docText = docText.Substring(0, 3997) + "...";
        return new RoadmapContext
        {
            AssignmentTitle = assignment.Title,
            DueDate = assignment.DueDate.ToString("yyyy-MM-dd"),
            AssignmentDescription = assignment.Description,
            DocumentText = docText,
            RoutineSummary = routineSummary
        };
    }

    private static RoadmapStep ReadRoadmapStep(MySqlDataReader r, bool includeTime = true, bool full = false)
    {
        var step = new RoadmapStep
        {
            Id = r.GetInt32(r.GetOrdinal("id")),
            AssignmentId = r.GetInt32(r.GetOrdinal("assignment_id")),
            StepOrder = r.GetInt32(r.GetOrdinal("step_order")),
            StepTitle = r.GetString(r.GetOrdinal("step_title")),
            SuggestedDate = r.IsDBNull(r.GetOrdinal("suggested_date")) ? null : r.GetDateTime(r.GetOrdinal("suggested_date")),
            Completed = r.GetByte(r.GetOrdinal("completed")) != 0
        };
        if (includeTime) try { if (!r.IsDBNull(r.GetOrdinal("suggested_time"))) step.SuggestedTime = GetTimeSpan(r, "suggested_time"); } catch { }
        if (full)
        {
            try { var ord = r.GetOrdinal("step_detail"); if (!r.IsDBNull(ord)) step.StepDetail = r.GetString(ord); } catch { }
            try { if (!r.IsDBNull(r.GetOrdinal("suggested_end_time"))) step.SuggestedEndTime = GetTimeSpan(r, "suggested_end_time"); } catch { }
        }
        return step;
    }

    private async Task<List<Assignment>> GetAssignmentsSortedByPriorityAsync(int userId)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(
            "SELECT id, user_id, title, description, due_date, priority, status, document_path, document_text, created_at, updated_at FROM assignments WHERE user_id = @u AND status != 'completed' ORDER BY due_date, FIELD(priority,'high','medium','low')", conn);
        cmd.Parameters.AddWithValue("@u", userId);
        var list = new List<Assignment>();
        using var r = (MySqlDataReader)await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add(ReadAssignment(r));
        return list;
    }

    private async Task<Assignment?> GetAssignmentByIdAsync(int id, int userId)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(
            "SELECT id, user_id, title, description, due_date, priority, status, document_path, document_text, created_at, updated_at FROM assignments WHERE id = @id AND user_id = @u", conn);
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@u", userId);
        using var r = (MySqlDataReader)await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? ReadAssignment(r) : null;
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
        try
        {
            var pathOrd = r.GetOrdinal("document_path");
            var textOrd = r.GetOrdinal("document_text");
            if (!r.IsDBNull(pathOrd)) a.DocumentPath = r.GetString(pathOrd);
            if (!r.IsDBNull(textOrd)) a.DocumentText = r.GetString(textOrd);
        }
        catch {  }
        return a;
    }

    private async Task<List<RoutineSlot>> GetRoutineSlotsAsync(int userId)
    {
        using var conn = _db.GetConnection();
        await conn.OpenAsync();
        using var cmd = new MySqlCommand(
            "SELECT id, user_id, day_of_week, start_time, end_time, title, description, created_at, updated_at FROM routine_slots WHERE user_id = @u", conn);
        cmd.Parameters.AddWithValue("@u", userId);
        var list = new List<RoutineSlot>();
        using var r = (MySqlDataReader)await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(ReadSlot(r));
        return list;
    }

    private static RoutineSlot ReadSlot(MySqlDataReader r) => new()
    {
        Id = r.GetInt32(r.GetOrdinal("id")),
        UserId = r.GetInt32(r.GetOrdinal("user_id")),
        DayOfWeek = r.GetInt32(r.GetOrdinal("day_of_week")),
        StartTime = GetTimeSpan(r, "start_time"),
        EndTime = GetTimeSpan(r, "end_time"),
        Title = r.GetString(r.GetOrdinal("title")),
        Description = r.IsDBNull(r.GetOrdinal("description")) ? null : r.GetString(r.GetOrdinal("description")),
        CreatedAt = r.GetDateTime(r.GetOrdinal("created_at")),
        UpdatedAt = r.GetDateTime(r.GetOrdinal("updated_at"))
    };

    private static TimeSpan GetTimeSpan(MySqlDataReader r, string col)
    {
        var val = r.GetValue(r.GetOrdinal(col));
        if (val is TimeSpan ts) return ts;
        if (val is DateTime dt) return dt.TimeOfDay;
        return TimeSpan.Zero;
    }

    private static List<(DateTime start, DateTime end)> GetFreeWindows(DateTime date, List<RoutineSlot> dayRoutine)
    {
        var busy = dayRoutine
            .Select(r => (date.Date.Add(r.StartTime), date.Date.Add(r.EndTime)))
            .OrderBy(x => x.Item1)
            .ToList();
        var free = new List<(DateTime, DateTime)>();
        var dayStart = date.Date.AddHours(8);
        var dayEnd = date.Date.AddHours(22);
        var current = dayStart;
        foreach (var (s, e) in busy)
        {
            if (current < s && (s - current).TotalMinutes >= 30)
                free.Add((current, s));
            if (e > current) current = e;
        }
        if (current < dayEnd && (dayEnd - current).TotalMinutes >= 30)
            free.Add((current, dayEnd));
        return free;
    }

    private static async Task ClearAiSchedulesForUserAsync(MySqlConnection conn, int userId, DateTime from, DateTime to)
    {
        using var cmd = new MySqlCommand("DELETE FROM ai_schedules WHERE user_id = @u AND slot_date >= @f AND slot_date <= @t", conn);
        cmd.Parameters.AddWithValue("@u", userId);
        cmd.Parameters.AddWithValue("@f", from.Date);
        cmd.Parameters.AddWithValue("@t", to.Date);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task InsertAiScheduleAsync(MySqlConnection conn, int userId, int assignmentId, DateTime slotDate, TimeSpan start, TimeSpan end, string notes)
    {
        using var cmd = new MySqlCommand(
            "INSERT INTO ai_schedules (user_id, assignment_id, slot_date, start_time, end_time, notes) VALUES (@u, @a, @d, @s, @e, @n)", conn);
        cmd.Parameters.AddWithValue("@u", userId);
        cmd.Parameters.AddWithValue("@a", assignmentId);
        cmd.Parameters.AddWithValue("@d", slotDate.Date);
        cmd.Parameters.AddWithValue("@s", start);
        cmd.Parameters.AddWithValue("@e", end);
        cmd.Parameters.AddWithValue("@n", notes);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task DeleteRoadmapStepsAsync(MySqlConnection conn, int assignmentId)
    {
        using var cmd = new MySqlCommand("DELETE FROM assignment_roadmap_steps WHERE assignment_id = @a", conn);
        cmd.Parameters.AddWithValue("@a", assignmentId);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<int> InsertRoadmapStepAsync(MySqlConnection conn, int assignmentId, int order, string title, DateTime suggestedDate, TimeSpan? suggestedTime = null, string? stepDetail = null, TimeSpan? suggestedEndTime = null)
    {
        try
        {
            using var cmd = new MySqlCommand(
                "INSERT INTO assignment_roadmap_steps (assignment_id, step_order, step_title, step_detail, suggested_date, suggested_time, suggested_end_time) VALUES (@a, @o, @t, @sd, @d, @st, @set)", conn);
            cmd.Parameters.AddWithValue("@a", assignmentId);
            cmd.Parameters.AddWithValue("@o", order);
            cmd.Parameters.AddWithValue("@t", title);
            cmd.Parameters.AddWithValue("@sd", (object?)stepDetail ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@d", suggestedDate.Date);
            cmd.Parameters.AddWithValue("@st", (object?)suggestedTime ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@set", (object?)suggestedEndTime ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
            return (int)cmd.LastInsertedId;
        }
        catch (MySqlException)
        {
            try
            {
                using var cmd = new MySqlCommand(
                    "INSERT INTO assignment_roadmap_steps (assignment_id, step_order, step_title, suggested_date, suggested_time) VALUES (@a, @o, @t, @d, @st)", conn);
                cmd.Parameters.AddWithValue("@a", assignmentId);
                cmd.Parameters.AddWithValue("@o", order);
                cmd.Parameters.AddWithValue("@t", title);
                cmd.Parameters.AddWithValue("@d", suggestedDate.Date);
                cmd.Parameters.AddWithValue("@st", (object?)suggestedTime ?? DBNull.Value);
                await cmd.ExecuteNonQueryAsync();
                return (int)cmd.LastInsertedId;
            }
            catch (MySqlException)
            {
                using var cmd = new MySqlCommand(
                    "INSERT INTO assignment_roadmap_steps (assignment_id, step_order, step_title, suggested_date) VALUES (@a, @o, @t, @d)", conn);
                cmd.Parameters.AddWithValue("@a", assignmentId);
                cmd.Parameters.AddWithValue("@o", order);
                cmd.Parameters.AddWithValue("@t", title);
                cmd.Parameters.AddWithValue("@d", suggestedDate.Date);
                await cmd.ExecuteNonQueryAsync();
                return (int)cmd.LastInsertedId;
            }
        }
    }
}
