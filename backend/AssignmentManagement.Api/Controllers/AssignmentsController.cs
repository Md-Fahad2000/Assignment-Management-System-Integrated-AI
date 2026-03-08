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

    public AssignmentsController(IAssignmentService assignments, IDocumentExtractService documentExtract, IWebHostEnvironment env, IRoutineService routine, IAssignmentRoadmapGeminiService roadmapGemini, IAssignmentRoadmapOpenAiService roadmapOpenAi, IAssignmentRoadmapOpenRouterService roadmapOpenRouter, IAssignmentRoadmapGroqService roadmapGroq)
    {
        _assignments = assignments;
        _documentExtract = documentExtract;
        _env = env;
        _routine = routine;
        _roadmapGemini = roadmapGemini;
        _roadmapOpenAi = roadmapOpenAi;
        _roadmapOpenRouter = roadmapOpenRouter;
        _roadmapGroq = roadmapGroq;
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

    /// <summary>Upload assignment document (PDF or TXT). AI roadmap will use this to create date-wise steps.</summary>
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

    /// <summary>Generate AI roadmap from PDF text + routine + deadline. Saves result to assignment.roadmap_json.</summary>
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
