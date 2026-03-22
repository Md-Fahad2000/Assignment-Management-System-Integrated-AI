using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AssignmentManagement.Api.Services;

public class RoadmapStepDto
{
    public string step_title { get; set; } = "";
    /// <summary>.</summary>
    public string? step_detail { get; set; }
    public string suggested_date { get; set; } = ""; 
    public string suggested_time { get; set; } = ""; 
    public string? suggested_end_time { get; set; }  
}

public interface IOpenAiRoadmapService
{
    Task<List<RoadmapStepDto>?> GetRoadmapFromAiAsync(string assignmentTitle, string? documentText, DateTime deadline, string routineSummary, string? assignmentDescription = null);
}

public class OpenAiRoadmapService : IOpenAiRoadmapService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpFactory;

    public OpenAiRoadmapService(IConfiguration config, IHttpClientFactory httpFactory)
    {
        _config = config;
        _httpFactory = httpFactory;
    }

    public async Task<List<RoadmapStepDto>?> GetRoadmapFromAiAsync(string assignmentTitle, string? documentText, DateTime deadline, string routineSummary, string? assignmentDescription = null)
    {
        var apiKey = _config["OpenAI:ApiKey"] ?? _config["Ai:OpenAiApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) return null;

        var doc = string.IsNullOrWhiteSpace(documentText) ? "No document provided." : documentText.Length > 6000 ? documentText.Substring(0, 6000) + "..." : documentText;
        var desc = string.IsNullOrWhiteSpace(assignmentDescription) ? "" : $"Assignment description: {assignmentDescription}\n\n";
        var today = DateTime.Today;
        var daysTotal = Math.Max(1, (deadline.Date - today).Days);
        var minSteps = Math.Max(4, Math.Min(8, daysTotal * 2));
        var maxSteps = Math.Min(25, Math.Max(8, daysTotal * 4));
        var prompt = $@"You are a study planner. The student must complete this assignment by the deadline. Create a DETAILED day-by-day roadmap.

Assignment title: {assignmentTitle}
Deadline: {deadline:yyyy-MM-dd}
Today: {today:yyyy-MM-dd}
There are {daysTotal} days. Spread work across Day 1 to Day {daysTotal}. Use {minSteps} to {maxSteps} steps (about 2-4 per day).
ROUTINE = BUSY (do not suggest study during routine). Only suggest FREE time. Give each step a DIFFERENT suggested_date/suggested_time. Routine (BUSY): {routineSummary}

{desc}Assignment content:
{doc}

Return ONLY a valid JSON array. Each step must have:
- step_title: Include ""Day N"" and optional ""Morning""/""Evening"" and action (e.g. ""Day 1 – Evening: Set up project and database"")
- step_detail: RICH and DETAILED: technical steps, tools/commands if relevant, what to deliver, suggested duration. 2-5 sentences.
- suggested_date: YYYY-MM-DD (spread from today to deadline) – different for each step.
- suggested_time: HH:mm start (24h) – FREE time only, not during routine.
- suggested_end_time: HH:mm end (24h)

Return {minSteps} to {maxSteps} steps. Every step must have step_detail and suggested_end_time. No duplicate date+time across steps.";

        var client = _httpFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);
        var body = new
        {
            model = "gpt-4o-mini",
            messages = new[] { new { role = "user", content = prompt } },
            temperature = 0.3
        };
        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
        if (!response.IsSuccessStatusCode) return null;

        var resJson = await response.Content.ReadAsStringAsync();
        using var doc2 = JsonDocument.Parse(resJson);
        var choices = doc2.RootElement.GetProperty("choices");
        if (choices.GetArrayLength() == 0) return null;
        var text = choices[0].GetProperty("message").GetProperty("content").GetString();
        if (string.IsNullOrWhiteSpace(text)) return null;

        text = text.Trim();
        var list = GeminiRoadmapService.ParseStepsFromJson(text);
        if (list != null)
            foreach (var s in list)
                if (string.IsNullOrWhiteSpace(s.step_title)) s.step_title = "Complete this step";
        return list;
    }
}
