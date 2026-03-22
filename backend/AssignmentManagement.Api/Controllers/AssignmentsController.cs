using AssignmentManagement.Api.Models;
using AssignmentManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AssignmentManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssignmentsController : ControllerBase
{
    private readonly IAssignmentService _assignments;
    private readonly IDocumentExtractService _documentExtract;
    private readonly IWebHostEnvironment _env;
    private readonly IRoutineService _routine;
    private readonly IAssignmentRoadmapGeminiService _roadmapGemini;
    private readonly IAssignmentRoadmapOpenAiService _roadmapOpenAi;
    private readonly IAssignmentRoadmapOpenRouterService _roadmapOpenRouter;
    private readonly IAssignmentRoadmapGroqService _roadmapGroq;
    private readonly IAssignmentAssistantService _assistant;
    private readonly IAssignmentChatService _chat;
    private readonly IAssignmentExtraService _extra;
    private readonly IAssignmentAiResponseService _aiResponse;

    public AssignmentsController(IAssignmentService assignments, IDocumentExtractService documentExtract, IWebHostEnvironment env, IRoutineService routine, IAssignmentRoadmapGeminiService roadmapGemini, IAssignmentRoadmapOpenAiService roadmapOpenAi, IAssignmentRoadmapOpenRouterService roadmapOpenRouter, IAssignmentRoadmapGroqService roadmapGroq, IAssignmentAssistantService assistant, IAssignmentChatService chat, IAssignmentExtraService extra, IAssignmentAiResponseService aiResponse)
    {
        _assignments = assignments;
        _documentExtract = documentExtract;
        _env = env;
        _routine = routine;
        _roadmapGemini = roadmapGemini;
        _roadmapOpenAi = roadmapOpenAi;
        _roadmapOpenRouter = roadmapOpenRouter;
        _roadmapGroq = roadmapGroq;
        _assistant = assistant;
        _chat = chat;
        _extra = extra;
        _aiResponse = aiResponse;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _assignments.GetByUserIdAsync(UserId);
        return Ok(list.Select(a => Map(a)));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var a = await _assignments.GetByIdAsync(id, UserId);
        if (a == null) return NotFound();
        return Ok(Map(a));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAssignmentRequest model)
    {
        var a = await _assignments.CreateAsync(UserId, model);
        if (a == null) return BadRequest(new { message = "Invalid input." });
        return Ok(Map(a));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAssignmentRequest model)
    {
        var a = await _assignments.UpdateAsync(id, UserId, model);
        if (a == null) return NotFound();
        return Ok(Map(a));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await _assignments.DeleteAsync(id, UserId)) return NotFound();
        return NoContent();
    }

    /// <summary>.</summary>
    [HttpPost("{id:int}/document")]
    public async Task<IActionResult> UploadDocument(int id, IFormFile file)
    {
        var assignment = await _assignments.GetByIdAsync(id, UserId);
        if (assignment == null) return NotFound();
        if (file == null || file.Length == 0) return BadRequest(new { message = "No file uploaded." });
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".pdf" && ext != ".txt") return BadRequest(new { message = "Only PDF and TXT files are allowed." });

        var uploadsDir = Path.Combine(_env.ContentRootPath, "Uploads", "Assignments", id.ToString());
        Directory.CreateDirectory(uploadsDir);
        var safeName = Path.GetFileName(file.FileName) ?? "document" + ext;
        var filePath = Path.Combine(uploadsDir, safeName);
        await using (var fs = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(fs);

        string? text;
        await using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            text = await _documentExtract.ExtractTextAsync(fs, file.FileName);
        var relativePath = Path.Combine("Assignments", id.ToString(), safeName);
        await _assignments.SetDocumentAsync(id, UserId, relativePath, text);
        return Ok(new { message = "Document uploaded.", hasText = !string.IsNullOrWhiteSpace(text) });
    }

    /// <summary>.</summary>
    [HttpPost("{id:int}/generate-roadmap")]
    public async Task<IActionResult> GenerateRoadmap(int id)
    {
        var assignment = await _assignments.GetByIdAsync(id, UserId);
        if (assignment == null) return NotFound();
        var routineSlots = await _routine.GetByUserIdAsync(UserId);
        var routineSummary = routineSlots.Count == 0 ? "No fixed routine." : string.Join("; ", routineSlots
            .GroupBy(r => r.DayOfWeek)
            .OrderBy(g => g.Key)
            .Select(g => new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" }[g.Key] + ": " + string.Join(", ", g.Select(s => s.StartTime.ToString(@"hh\:mm") + "-" + s.EndTime.ToString(@"hh\:mm") + " " + s.Title))));
        var pdfText = assignment.DocumentText ?? "";
        var deadline = assignment.DueDate.Date;
        string? roadmapJson = null;
        string? lastError = null;

        try
        {
            roadmapJson = await _roadmapGemini.GenerateRoadmapJsonAsync(pdfText, routineSummary, deadline);
        }
        catch (InvalidOperationException ex) { lastError = ex.Message; }
        catch (OperationCanceledException) { lastError = "Request timed out."; }
        catch (Exception ex) { lastError = ex.Message; }

        if (string.IsNullOrWhiteSpace(roadmapJson))
        {
            try
            {
                roadmapJson = await _roadmapOpenAi.GenerateRoadmapJsonAsync(pdfText, routineSummary, deadline);
            }
            catch (Exception ex) { lastError = lastError ?? ex.Message; }
        }

        if (string.IsNullOrWhiteSpace(roadmapJson))
        {
            try
            {
                roadmapJson = await _roadmapOpenRouter.GenerateRoadmapJsonAsync(pdfText, routineSummary, deadline);
            }
            catch (Exception ex) { lastError = lastError ?? ex.Message; }
        }

        if (string.IsNullOrWhiteSpace(roadmapJson))
        {
            try
            {
                roadmapJson = await _roadmapGroq.GenerateRoadmapJsonAsync(pdfText, routineSummary, deadline);
            }
            catch (Exception ex) { lastError = lastError ?? ex.Message; }
        }

        if (!string.IsNullOrWhiteSpace(roadmapJson))
        {
            await _assignments.SetRoadmapJsonAsync(id, UserId, roadmapJson);
            return Ok(new { roadmap_json = roadmapJson, message = "Roadmap generated." });
        }

        return BadRequest(new { message = lastError ?? "AI could not generate roadmap. Add at least one API key: Gemini, OpenAI, OpenRouter, or Groq in appsettings." });
    }

    /// <summary>.</summary>
    [HttpGet("{id:int}/assistant/history")]
    public async Task<IActionResult> GetAssistantHistory(int id)
    {
        var assignment = await _assignments.GetByIdAsync(id, UserId);
        if (assignment == null) return NotFound();
        var messages = await _chat.GetMessagesAsync(id, UserId);
        return Ok(new { messages });
    }

    /// <summary>.</summary>
    [HttpPost("{id:int}/assistant")]
    public async Task<IActionResult> AskAssistant(int id, [FromBody] AssignmentAssistantRequest request)
    {
        var assignment = await _assignments.GetByIdAsync(id, UserId);
        if (assignment == null) return NotFound();
        var routineSlots = await _routine.GetByUserIdAsync(UserId);
        var routineSummary = routineSlots.Count == 0 ? "No fixed routine." : string.Join("; ", routineSlots
            .GroupBy(r => r.DayOfWeek)
            .OrderBy(g => g.Key)
            .Select(g => new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" }[g.Key] + ": " + string.Join(", ", g.Select(s => s.StartTime.ToString(@"hh\:mm") + "-" + s.EndTime.ToString(@"hh\:mm") + " " + s.Title))));
        try
        {
            List<AssignmentAssistantMessage> msgs;
            if (!string.IsNullOrWhiteSpace(request?.Message))
            {
                var history = await _chat.GetMessagesAsync(id, UserId);
                msgs = history.ToList();
                msgs.Add(new AssignmentAssistantMessage { Role = "user", Content = request.Message });
            }
            else
            {
                msgs = request?.Messages ?? new List<AssignmentAssistantMessage>();
            }

            var answer = await _assistant.AskAsync(assignment, routineSummary, msgs);
            if (string.IsNullOrWhiteSpace(answer))
                return BadRequest(new { message = "AI assistant returned empty response. Try again." });

            int? assistantMessageId = null;
            if (!string.IsNullOrWhiteSpace(request?.Message))
            {
                await _chat.AddMessageAsync(id, UserId, "user", request.Message);
                assistantMessageId = await _chat.AddMessageAsync(id, UserId, "assistant", answer);
            }

            return Ok(new { answer, assistantMessageId });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (OperationCanceledException)
        {
            return BadRequest(new { message = "Request timed out. Please try again." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>.</summary>
    [HttpGet("{id:int}/ai-responses")]
    public async Task<IActionResult> GetAiResponses(int id)
    {
        var assignment = await _assignments.GetByIdAsync(id, UserId);
        if (assignment == null) return NotFound();
        var responses = await _aiResponse.GetAllAsync(id, UserId);
        return Ok(responses);
    }

    /// <summary>.</summary>
    [HttpGet("{id:int}/summarize")]
    public async Task<IActionResult> Summarize(int id)
    {
        var assignment = await _assignments.GetByIdAsync(id, UserId);
        if (assignment == null) return NotFound();
        try
        {
            var summary = await _extra.SummarizeAsync(assignment);
            if (string.IsNullOrWhiteSpace(summary)) return BadRequest(new { message = "AI could not generate summary." });
            await _aiResponse.SetAsync(id, UserId, "summary", summary);
            return Ok(new { summary });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (OperationCanceledException) { return BadRequest(new { message = "Request timed out." }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>.</summary>
    [HttpGet("{id:int}/suggest-slots")]
    public async Task<IActionResult> SuggestSlots(int id, [FromQuery] bool detailed = false)
    {
        var assignment = await _assignments.GetByIdAsync(id, UserId);
        if (assignment == null) return NotFound();
        var routineSlots = await _routine.GetByUserIdAsync(UserId);
        var routineSummary = routineSlots.Count == 0 ? "No fixed routine." : string.Join("; ", routineSlots
            .GroupBy(r => r.DayOfWeek)
            .OrderBy(g => g.Key)
            .Select(g => new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" }[g.Key] + ": " + string.Join(", ", g.Select(s => s.StartTime.ToString(@"hh\:mm") + "-" + s.EndTime.ToString(@"hh\:mm") + " " + s.Title))));
        try
        {
            if (detailed)
            {
                var textR = await _extra.GetSuggestedSlotsWithReasonsAsync(assignment, routineSummary);
                if (string.IsNullOrWhiteSpace(textR)) return BadRequest(new { message = "AI could not suggest slots." });
                var lines = textR.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
                var slotsWithReason = lines.Select(line =>
                {
                    var idx = line.LastIndexOf(" - ", StringComparison.Ordinal);
                    if (idx > 0) return new { text = line.Substring(0, idx).Trim(), reason = line.Substring(idx + 3).Trim() };
                    return new { text = line, reason = "" };
                }).ToArray();
                await _aiResponse.SetAsync(id, UserId, "suggested_slots", System.Text.Json.JsonSerializer.Serialize(slotsWithReason));
                return Ok(new { slots = slotsWithReason });
            }
            var slotText = await _extra.GetSuggestedSlotsAsync(assignment, routineSummary);
            if (string.IsNullOrWhiteSpace(slotText)) return BadRequest(new { message = "AI could not suggest slots." });
            var list = slotText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
            await _aiResponse.SetAsync(id, UserId, "suggested_slots", System.Text.Json.JsonSerializer.Serialize(list));
            return Ok(new { slots = list });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (OperationCanceledException) { return BadRequest(new { message = "Request timed out." }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("{id:int}/suggest-title")]
    public async Task<IActionResult> SuggestTitle(int id)
    {
        var assignment = await _assignments.GetByIdAsync(id, UserId);
        if (assignment == null) return NotFound();
        try
        {
            var title = await _extra.SuggestTitleFromDocumentAsync(assignment);
            if (string.IsNullOrWhiteSpace(title)) return BadRequest(new { message = "No document or AI could not suggest title." });
            var t = title.Trim();
            await _aiResponse.SetAsync(id, UserId, "suggested_title", t);
            return Ok(new { title = t });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("{id:int}/key-points")]
    public async Task<IActionResult> KeyPoints(int id)
    {
        var assignment = await _assignments.GetByIdAsync(id, UserId);
        if (assignment == null) return NotFound();
        try
        {
            var text = await _extra.GetKeyPointsAsync(assignment);
            if (string.IsNullOrWhiteSpace(text)) return BadRequest(new { message = "AI could not extract key points." });
            var points = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
            await _aiResponse.SetAsync(id, UserId, "key_points", System.Text.Json.JsonSerializer.Serialize(points));
            return Ok(new { points });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{id:int}/simplify-roadmap")]
    public async Task<IActionResult> SimplifyRoadmap(int id)
    {
        var assignment = await _assignments.GetByIdAsync(id, UserId);
        if (assignment == null) return NotFound();
        try
        {
            var roadmapJson = await _extra.SimplifyRoadmapAsync(assignment);
            if (string.IsNullOrWhiteSpace(roadmapJson)) return BadRequest(new { message = "No roadmap or AI could not simplify." });
            return Ok(new { roadmap_json = roadmapJson.Trim() });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id:int}/roadmap")]
    public async Task<IActionResult> SetRoadmap(int id, [FromBody] SetRoadmapRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.RoadmapJson)) return BadRequest(new { message = "roadmap_json required." });
        var assignment = await _assignments.GetByIdAsync(id, UserId);
        if (assignment == null) return NotFound();
        var ok = await _assignments.SetRoadmapJsonAsync(id, UserId, request.RoadmapJson);
        if (!ok) return NotFound();
        return Ok(new { message = "Roadmap updated." });
    }

    [HttpGet("{id:int}/roadmap-conflicts")]
    public async Task<IActionResult> RoadmapConflicts(int id)
    {
        var assignment = await _assignments.GetByIdAsync(id, UserId);
        if (assignment == null) return NotFound();
        var routineSlots = await _routine.GetByUserIdAsync(UserId);
        var conflicts = GetRoadmapConflicts(assignment.RoadmapJson, routineSlots);
        return Ok(new { conflicts });
    }

    private static List<object> GetRoadmapConflicts(string? roadmapJson, List<RoutineSlot> routineSlots)
    {
        var result = new List<object>();
        if (string.IsNullOrWhiteSpace(roadmapJson) || routineSlots.Count == 0) return result;
        List<RoadmapStepJson>? steps;
        try { steps = System.Text.Json.JsonSerializer.Deserialize<List<RoadmapStepJson>>(roadmapJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }); } catch { return result; }
        if (steps == null || steps.Count == 0) return result;
        var dayNames = new[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            var startStr = step.StartDate ?? step.startDate ?? "";
            var endStr = step.EndDate ?? step.endDate ?? startStr;
            if (string.IsNullOrWhiteSpace(startStr)) continue;
            if (!DateTime.TryParse(startStr, out var stepStart)) continue;
            var stepEnd = string.IsNullOrWhiteSpace(endStr) ? stepStart.AddHours(1) : (DateTime.TryParse(endStr, out var e) ? e : stepStart.AddHours(1));
            var stepDay = (int)stepStart.DayOfWeek;
            var stepStartTime = stepStart.TimeOfDay;
            var stepEndTime = stepEnd.TimeOfDay;
            foreach (var slot in routineSlots.Where(s => s.DayOfWeek == stepDay))
            {
                if (stepStartTime < slot.EndTime && stepEndTime > slot.StartTime)
                {
                    result.Add(new
                    {
                        stepIndex = i + 1,
                        stepTitle = step.Step ?? step.step ?? $"Step {i + 1}",
                        conflictWith = $"{dayNames[slot.DayOfWeek]} {slot.StartTime.ToString(@"hh\:mm")}-{slot.EndTime.ToString(@"hh\:mm")} {slot.Title}"
                    });
                    break;
                }
            }
        }
        return result;
    }

    [HttpPost("{id:int}/assistant/suggest-follow-ups")]
    public async Task<IActionResult> SuggestFollowUps(int id, [FromBody] SuggestFollowUpsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.LastReply)) return BadRequest(new { message = "lastReply required." });
        try
        {
            var text = await _extra.GetSuggestedFollowUpsAsync(request.LastReply);
            if (string.IsNullOrWhiteSpace(text)) return BadRequest(new { message = "Could not generate." });
            var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0).Take(3).ToArray();
            return Ok(new { followUps = lines });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("{id:int}/key-dates")]
    public async Task<IActionResult> KeyDates(int id)
    {
        var assignment = await _assignments.GetByIdAsync(id, UserId);
        if (assignment == null) return NotFound();
        try
        {
            var text = await _extra.GetKeyDatesFromDocumentAsync(assignment);
            var lines = string.IsNullOrWhiteSpace(text) ? Array.Empty<string>() : text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
            await _aiResponse.SetAsync(id, UserId, "key_dates", System.Text.Json.JsonSerializer.Serialize(lines));
            return Ok(new { dates = lines });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("{id:int}/quiz")]
    public async Task<IActionResult> Quiz(int id)
    {
        var assignment = await _assignments.GetByIdAsync(id, UserId);
        if (assignment == null) return NotFound();
        try
        {
            var text = await _extra.GetQuizFromAssignmentAsync(assignment);
            var quizVal = text ?? "";
            if (!string.IsNullOrWhiteSpace(quizVal)) await _aiResponse.SetAsync(id, UserId, "quiz", quizVal);
            return Ok(new { quiz = quizVal });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("{id:int}/one-line-summary")]
    public async Task<IActionResult> OneLineSummary(int id)
    {
        var assignment = await _assignments.GetByIdAsync(id, UserId);
        if (assignment == null) return NotFound();
        try
        {
            var text = await _extra.GetOneLineSummaryAsync(assignment);
            var oneLine = text ?? "";
            if (!string.IsNullOrWhiteSpace(oneLine)) await _aiResponse.SetAsync(id, UserId, "one_line_summary", oneLine);
            return Ok(new { summary = oneLine });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("{id:int}/reschedule-roadmap")]
    public async Task<IActionResult> RescheduleRoadmap(int id, [FromBody] RescheduleRoadmapRequest request)
    {
        var assignment = await _assignments.GetByIdAsync(id, UserId);
        if (assignment == null) return NotFound();
        var routineSlots = await _routine.GetByUserIdAsync(UserId);
        var routineSummary = routineSlots.Count == 0 ? "No routine." : string.Join("; ", routineSlots.GroupBy(r => r.DayOfWeek).OrderBy(g => g.Key).Select(g => new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" }[g.Key] + ": " + string.Join(", ", g.Select(s => s.StartTime.ToString(@"hh\:mm") + "-" + s.EndTime.ToString(@"hh\:mm") + " " + s.Title))));
        try
        {
            var fromStep = request?.FromStepIndex ?? 0;
            var roadmapJson = await _extra.RescheduleRoadmapFromStepAsync(assignment, fromStep, routineSummary);
            if (string.IsNullOrWhiteSpace(roadmapJson)) return BadRequest(new { message = "No roadmap or could not reschedule." });
            return Ok(new { roadmap_json = roadmapJson.Trim() });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("{id:int}/explain-step")]
    public async Task<IActionResult> ExplainStep(int id, [FromQuery] string stepTitle, [FromQuery] string? stepDescription = null)
    {
        var assignment = await _assignments.GetByIdAsync(id, UserId);
        if (assignment == null) return NotFound();
        if (string.IsNullOrWhiteSpace(stepTitle)) return BadRequest(new { message = "stepTitle required." });
        try
        {
            var text = await _extra.ExplainStepInPlaceAsync(assignment, stepTitle, stepDescription);
            return Ok(new { explanation = text ?? "" });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("{id:int}/difficulty-time")]
    public async Task<IActionResult> DifficultyTime(int id)
    {
        var assignment = await _assignments.GetByIdAsync(id, UserId);
        if (assignment == null) return NotFound();
        try
        {
            var json = await _extra.GetDifficultyAndTimePerStepAsync(assignment);
            if (string.IsNullOrWhiteSpace(json)) return BadRequest(new { message = "No roadmap." });
            return Ok(new { roadmap_json = json.Trim() });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("{id:int}/best-day")]
    public async Task<IActionResult> BestDay(int id)
    {
        var assignment = await _assignments.GetByIdAsync(id, UserId);
        if (assignment == null) return NotFound();
        var routineSlots = await _routine.GetByUserIdAsync(UserId);
        var routineSummary = routineSlots.Count == 0 ? "No routine." : string.Join("; ", routineSlots.GroupBy(r => r.DayOfWeek).OrderBy(g => g.Key).Select(g => new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" }[g.Key] + ": " + string.Join(", ", g.Select(s => s.StartTime.ToString(@"hh\:mm") + "-" + s.EndTime.ToString(@"hh\:mm") + " " + s.Title))));
        try
        {
            var text = await _extra.GetBestDayAsync(assignment, routineSummary);
            var bestDayVal = text ?? "";
            if (!string.IsNullOrWhiteSpace(bestDayVal)) await _aiResponse.SetAsync(id, UserId, "best_day", bestDayVal);
            return Ok(new { suggestion = bestDayVal });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id:int}/assistant/messages/{messageId:int}/rate")]
    public async Task<IActionResult> RateMessage(int id, int messageId, [FromBody] RateMessageRequest request)
    {
        if (request?.Rating != 1 && request?.Rating != -1) return BadRequest(new { message = "rating must be 1 or -1." });
        var ok = await _chat.SetMessageRatingAsync(id, UserId, messageId, request!.Rating!.Value);
        return ok ? Ok(new { message = "Rated." }) : NotFound();
    }

    [HttpPost("{id:int}/conflict-alternative")]
    public async Task<IActionResult> ConflictAlternative(int id, [FromBody] ConflictAlternativeRequest request)
    {
        var assignment = await _assignments.GetByIdAsync(id, UserId);
        if (assignment == null) return NotFound();
        var routineSlots = await _routine.GetByUserIdAsync(UserId);
        var routineSummary = routineSlots.Count == 0 ? "No routine." : string.Join("; ", routineSlots.GroupBy(r => r.DayOfWeek).OrderBy(g => g.Key).Select(g => new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" }[g.Key] + ": " + string.Join(", ", g.Select(s => s.StartTime.ToString(@"hh\:mm") + "-" + s.EndTime.ToString(@"hh\:mm") + " " + s.Title))));
        if (request == null || request.StepIndex < 0 || string.IsNullOrWhiteSpace(request.ConflictWith)) return BadRequest(new { message = "stepIndex and conflictWith required." });
        try
        {
            var text = await _extra.GetConflictAlternativeAsync(assignment, request.StepIndex, request.ConflictWith, routineSummary);
            return Ok(new { suggestion = text ?? "" });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    private static object Map(Assignment a) => new
    {
        a.Id,
        a.UserId,
        a.Title,
        a.Description,
        due_date = a.DueDate.ToString("yyyy-MM-dd"),
        a.Priority,
        a.Status,
        document_path = a.DocumentPath,
        has_document = !string.IsNullOrEmpty(a.DocumentPath),
        roadmap_json = a.RoadmapJson,
        created_at = a.CreatedAt,
        updated_at = a.UpdatedAt
    };
}
