using System.Security.Claims;
using System.Text.Json;
using AssignmentManagement.Api.Models;
using AssignmentManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IAiSchedulingService _ai;

    public AiController(IAiSchedulingService ai) => _ai = ai;

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    /// <summary>.</summary>
    [HttpGet("schedule")]
    public async Task<IActionResult> GetSchedule([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var fromDate = from ?? DateTime.Today;
        var toDate = to ?? DateTime.Today.AddDays(14);
        var slots = await _ai.GetScheduleAsync(UserId, fromDate, toDate);
        return Ok(slots.Select(s => new
        {
            s.Id,
            s.AssignmentId,
            slot_date = s.SlotDate.ToString("yyyy-MM-dd"),
            start_time = s.StartTime.ToString(@"hh\:mm"),
            end_time = s.EndTime.ToString(@"hh\:mm"),
            s.Notes,
            s.AssignmentTitle
        }));
    }

    /// <summary>.</summary>
    [HttpPost("schedule/generate")]
    public async Task<IActionResult> GenerateSchedule([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var fromDate = from ?? DateTime.Today;
        var toDate = to ?? DateTime.Today.AddDays(14);
        if (fromDate > toDate) return BadRequest(new { message = "from must be <= to." });
        var slots = await _ai.GenerateScheduleAsync(UserId, fromDate, toDate);
        return Ok(slots.Select(s => new
        {
            s.Id,
            s.AssignmentId,
            slot_date = s.SlotDate.ToString("yyyy-MM-dd"),
            start_time = s.StartTime.ToString(@"hh\:mm"),
            end_time = s.EndTime.ToString(@"hh\:mm"),
            s.Notes,
            s.AssignmentTitle
        }));
    }

    /// <summary>.</summary>
    [HttpGet("priority-score")]
    public IActionResult GetPriorityScore([FromQuery] DateTime dueDate, [FromQuery] string priority = "medium")
    {
        var score = _ai.GetPriorityScore(dueDate, priority);
        return Ok(new { dueDate = dueDate.ToString("yyyy-MM-dd"), priority, score });
    }

    /// <summary>.</summary>
    [HttpGet("roadmap/{assignmentId:int}/context")]
    public async Task<IActionResult> GetRoadmapContext(int assignmentId)
    {
        var ctx = await _ai.GetRoadmapContextAsync(assignmentId, UserId);
        if (ctx == null) return NotFound(new { message = "Assignment not found." });
        return Ok(new
        {
            assignment_title = ctx.AssignmentTitle,
            due_date = ctx.DueDate,
            assignment_description = ctx.AssignmentDescription,
            document_text = ctx.DocumentText,
            routine_summary = ctx.RoutineSummary
        });
    }

    /// <summary>.</summary>
    [HttpGet("roadmap/{assignmentId:int}")]
    public async Task<IActionResult> GetRoadmap(int assignmentId)
    {
        var steps = await _ai.GetRoadmapAsync(assignmentId, UserId);
        return Ok(steps.Select(s => new
        {
            s.Id,
            s.AssignmentId,
            step_order = s.StepOrder,
            step_title = s.StepTitle ?? "Complete this step",
            step_detail = s.StepDetail,
            suggested_date = s.SuggestedDate?.ToString("yyyy-MM-dd"),
            suggested_time = s.SuggestedTime != null ? s.SuggestedTime.Value.ToString(@"hh\:mm") : null,
            suggested_end_time = s.SuggestedEndTime != null ? s.SuggestedEndTime.Value.ToString(@"hh\:mm") : null,
            s.Completed
        }));
    }

    /// <summary>.</summary>
    [HttpPost("roadmap/generate/{assignmentId:int}/stream")]
    public async Task StreamGenerateRoadmap(int assignmentId)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache, no-store";
        Response.Headers.Connection = "keep-alive";
        await Response.Body.FlushAsync();

        async Task WriteFinalEvent(IEnumerable<object> stepList, string? error = null)
        {
            var payload = new Dictionary<string, object> { ["done"] = true, ["steps"] = stepList };
            if (!string.IsNullOrEmpty(error)) payload["error"] = error;
            await Response.WriteAsync($"data: {JsonSerializer.Serialize(payload)}\n\n");
            await Response.Body.FlushAsync();
        }

        List<RoadmapStep> steps;
        using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(90)))
        using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted, timeoutCts.Token))
        {
            try
            {
                steps = await _ai.StreamGenerateRoadmapAsync(assignmentId, UserId, async chunk =>
                {
                    var data = JsonSerializer.Serialize(new { chunk });
                    await Response.WriteAsync($"data: {data}\n\n");
                    await Response.Body.FlushAsync();
                }, linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                await WriteFinalEvent(Array.Empty<object>(), "Request timed out or cancelled. Try non-streaming generate.");
                return;
            }
            catch (Exception ex)
            {
                await WriteFinalEvent(Array.Empty<object>(), ex.Message ?? "AI request failed.");
                return;
            }
        }

        var stepDtos = steps.Select(s => new
        {
            s.Id,
            s.AssignmentId,
            step_order = s.StepOrder,
            step_title = s.StepTitle ?? "Complete this step",
            step_detail = s.StepDetail,
            suggested_date = s.SuggestedDate?.ToString("yyyy-MM-dd"),
            suggested_time = s.SuggestedTime != null ? s.SuggestedTime.Value.ToString(@"hh\:mm") : null,
            suggested_end_time = s.SuggestedEndTime != null ? s.SuggestedEndTime.Value.ToString(@"hh\:mm") : null,
            s.Completed
        });
        await WriteFinalEvent(stepDtos);
    }

    /// <summary>.</summary>
    [HttpPost("roadmap/generate/{assignmentId:int}")]
    public async Task<IActionResult> GenerateRoadmap(int assignmentId)
    {
        var steps = await _ai.GenerateRoadmapAsync(assignmentId, UserId);
        if (steps.Count == 0) return NotFound(new { message = "Assignment not found." });
        return Ok(steps.Select(s => new
        {
            s.Id,
            s.AssignmentId,
            step_order = s.StepOrder,
            step_title = s.StepTitle ?? "Complete this step",
            step_detail = s.StepDetail,
            suggested_date = s.SuggestedDate?.ToString("yyyy-MM-dd"),
            suggested_time = s.SuggestedTime != null ? s.SuggestedTime.Value.ToString(@"hh\:mm") : null,
            suggested_end_time = s.SuggestedEndTime != null ? s.SuggestedEndTime.Value.ToString(@"hh\:mm") : null,
            s.Completed
        }));
    }
}
