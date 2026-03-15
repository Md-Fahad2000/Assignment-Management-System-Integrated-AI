using AssignmentManagement.Api.Models;
using AssignmentManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AssignmentManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IAssignmentService _assignments;
    private readonly IRoutineService _routine;
    private readonly IAssignmentExtraService _extra;
    private readonly IStressService _stress;

    public DashboardController(IAssignmentService assignments, IRoutineService routine, IAssignmentExtraService extra, IStressService stress)
    {
        _assignments = assignments;
        _routine = routine;
        _extra = extra;
        _stress = stress;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    /// <summary>.</summary>
    [HttpGet("focus-tip")]
    public async Task<IActionResult> FocusTip()
    {
        var assignments = await _assignments.GetByUserIdAsync(UserId);
        var routineSlots = await _routine.GetByUserIdAsync(UserId);
        var routineSummary = routineSlots.Count == 0 ? "No fixed routine." : string.Join("; ", routineSlots
            .GroupBy(r => r.DayOfWeek)
            .OrderBy(g => g.Key)
            .Select(g => new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" }[g.Key] + ": " + string.Join(", ", g.Select(s => s.StartTime.ToString(@"hh\:mm") + "-" + s.EndTime.ToString(@"hh\:mm") + " " + s.Title))));
        try
        {
            var tip = await _extra.GetFocusTipAsync(assignments, routineSummary);
            return Ok(new { tip = tip ?? "No assignments with roadmaps. Add assignments and generate roadmaps." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>.</summary>
    [HttpGet("weekly-plan")]
    public async Task<IActionResult> WeeklyPlan()
    {
        var assignments = await _assignments.GetByUserIdAsync(UserId);
        var routineSlots = await _routine.GetByUserIdAsync(UserId);
        var routineSummary = routineSlots.Count == 0 ? "No fixed routine." : string.Join("; ", routineSlots
            .GroupBy(r => r.DayOfWeek)
            .OrderBy(g => g.Key)
            .Select(g => new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" }[g.Key] + ": " + string.Join(", ", g.Select(s => s.StartTime.ToString(@"hh\:mm") + "-" + s.EndTime.ToString(@"hh\:mm") + " " + s.Title))));
        try
        {
            var plan = await _extra.GetWeeklyPlanAsync(assignments, routineSummary);
            return Ok(new { plan = plan ?? "No pending assignments." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>.</summary>
    [HttpGet("overload-warning")]
    public async Task<IActionResult> OverloadWarning()
    {
        var assignments = await _assignments.GetByUserIdAsync(UserId);
        var routineSlots = await _routine.GetByUserIdAsync(UserId);
        var today = DateTime.Today;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var weekEnd = weekStart.AddDays(7);
        double routineHours = 0;
        foreach (var slot in routineSlots)
        {
            var slotDuration = (slot.EndTime - slot.StartTime).TotalHours;
            routineHours += slotDuration; 
        }
        double roadmapHours = 0;
        foreach (var a in assignments.Where(x => x.Status != "completed" && !string.IsNullOrWhiteSpace(x.RoadmapJson)))
        {
            try
            {
                var steps = System.Text.Json.JsonSerializer.Deserialize<List<RoadmapStepJson>>(a.RoadmapJson ?? "[]", new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (steps == null) continue;
                foreach (var step in steps)
                {
                    var startStr = step.StartDate ?? step.startDate ?? "";
                    var endStr = step.EndDate ?? step.endDate ?? startStr;
                    if (string.IsNullOrWhiteSpace(startStr) || !DateTime.TryParse(startStr, out var start)) continue;
                    var end = string.IsNullOrWhiteSpace(endStr) ? start.AddHours(1) : (DateTime.TryParse(endStr, out var e) ? e : start.AddHours(1));
                    if (start >= weekStart && end <= weekEnd)
                        roadmapHours += (end - start).TotalHours;
                }
            }
            catch { }
        }
        var totalHours = routineHours + roadmapHours;
        const double threshold = 50;
        if (totalHours < threshold)
            return Ok(new { warning = (string?)null, suggestion = (string?)null, totalHours = Math.Round(totalHours, 1), routineHours = Math.Round(routineHours, 1), roadmapHours = Math.Round(roadmapHours, 1) });
        var message = $"This week: {Math.Round(totalHours, 0)} hours (routine + roadmap). Consider spreading deadlines or simplifying roadmaps.";
        string? suggestion = null;
        try { suggestion = await _extra.GetOverloadSuggestionAsync(assignments, message); } catch { }
        return Ok(new { warning = message, suggestion, totalHours = Math.Round(totalHours, 1), routineHours = Math.Round(routineHours, 1), roadmapHours = Math.Round(roadmapHours, 1) });
    }

    /// <summary>.</summary>
    [HttpGet("stress-summary")]
    public async Task<IActionResult> StressSummary()
    {
        var summary = await _stress.GetSummaryAsync(UserId);
        return Ok(new
        {
            previous_week = summary.PreviousWeek,
            current_week = summary.CurrentWeek,
            future_predicted = summary.FuturePredicted,
            message = summary.Message
        });
    }
}
