using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AssignmentManagement.Api.Services;

/// <summary>Google Gemini API (free tier) for roadmap steps. Get key at https://aistudio.google.com/app/apikey</summary>
public interface IGeminiRoadmapService
{
    Task<List<RoadmapStepDto>?> GetRoadmapFromAiAsync(string assignmentTitle, string? documentText, DateTime deadline, string routineSummary, string? assignmentDescription = null);
    IAsyncEnumerable<string> StreamRoadmapFromGeminiAsync(string assignmentTitle, string? documentText, DateTime deadline, string routineSummary, string? assignmentDescription = null, CancellationToken cancellationToken = default);
}

public class GeminiRoadmapService : IGeminiRoadmapService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<GeminiRoadmapService> _logger;

    public GeminiRoadmapService(IConfiguration config, IHttpClientFactory httpFactory, ILogger<GeminiRoadmapService> logger)
    {
        _config = config;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    private string GeminiModel => _config["Gemini:Model"] ?? _config["GoogleGemini:Model"] ?? "gemini-flash-latest";
    private HttpClient GetClient() => _httpFactory.CreateClient("Gemini");

    private static void SetGeminiAuth(HttpRequestMessage request, string apiKey)
    {
        request.Headers.TryAddWithoutValidation("X-goog-api-key", apiKey);
    }

    public async Task<List<RoadmapStepDto>?> GetRoadmapFromAiAsync(string assignmentTitle, string? documentText, DateTime deadline, string routineSummary, string? assignmentDescription = null)
    {
        var apiKey = _config["Gemini:ApiKey"] ?? _config["GoogleGemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) return null;

        var doc = string.IsNullOrWhiteSpace(documentText) ? "No document provided." : documentText.Length > 6000 ? documentText.Substring(0, 6000) + "..." : documentText;
        var desc = string.IsNullOrWhiteSpace(assignmentDescription) ? "" : $"Assignment description: {assignmentDescription}\n\n";
        var today = DateTime.Today;
        var daysTotal = Math.Max(1, (deadline.Date - today).Days);
        var minSteps = Math.Max(4, Math.Min(8, daysTotal * 2));
        var maxSteps = Math.Min(25, Math.Max(8, daysTotal * 4));

        var prompt = $@"You are a study planner. The student must complete this assignment by the deadline. Create a DETAILED day-by-day roadmap.

Assignment title: {assignmentTitle}
Deadline (must complete by): {deadline:yyyy-MM-dd}
Today: {today:yyyy-MM-dd}
There are {daysTotal} days until the deadline. Spread the work across Day 1, Day 2, ... Day {daysTotal}. Use between {minSteps} and {maxSteps} steps total (about 2-4 steps per day).
IMPORTANT – ROUTINE = BUSY (do not suggest study during these times). Only suggest FREE time. Routine (BUSY): {routineSummary}

{desc}Assignment document/content:
{doc}

Return ONLY a valid JSON array. No other text, no markdown. Each step must have:
- step_title: Start with ""Day N"" and optional ""Morning"" or ""Evening"" and a short action (e.g. ""Day 1 – Evening: Set up project and database"", ""Day 2 – Morning: Implement CRUD and business logic"")
- step_detail: RICH and DETAILED. Include: (1) exact technical steps and sub-tasks, (2) tools/commands if relevant (e.g. dotnet run, npm install), (3) what to deliver or check, (4) suggested duration or timing. Write 2-5 sentences so the student knows everything to do. Example: ""Create the GitHub repo. Initialize ASP.NET Core 10 with Individual Accounts. Add Book, Member, Loan models with navigation properties. Create DbInitializer; use Bogus to seed 20 books, 10 members, 15 loans and an Admin user. Run dotnet ef migrations add InitialCreate and update the database. Block: about 2 hours.""
- suggested_date: YYYY-MM-DD (spread from {today:yyyy-MM-dd} to {deadline:yyyy-MM-dd}) – use a DIFFERENT date/time for each step (no duplicates).
- suggested_time: HH:mm start (24h) – only FREE time, not during routine.
- suggested_end_time: HH:mm end (24h) – only FREE time.

Return {minSteps} to {maxSteps} steps. Every step must have step_detail and suggested_end_time. Give each step a DIFFERENT suggested_date or suggested_time so no two steps have the same date+time.";

        var model = GeminiModel;
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
        var body = new { contents = new[] { new { parts = new[] { new { text = prompt } } } }, generationConfig = new { temperature = 0.3f } };
        var json = JsonSerializer.Serialize(body);
        var client = GetClient();
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        SetGeminiAuth(request, apiKey);
        var response = await client.SendAsync(request);
        var resJson = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Gemini generateContent failed: {Status} {Body}", response.StatusCode, resJson.Length > 500 ? resJson.Substring(0, 500) + "..." : resJson);
            return null;
        }
        string? text = null;
        try
        {
            using var doc2 = JsonDocument.Parse(resJson);
            var candidates = doc2.RootElement.GetProperty("candidates");
            if (candidates.GetArrayLength() == 0) return null;
            var contentPart = candidates[0].GetProperty("content").GetProperty("parts");
            if (contentPart.GetArrayLength() == 0) return null;
            text = contentPart[0].GetProperty("text").GetString();
        }
        catch { return null; }

        if (string.IsNullOrWhiteSpace(text)) return null;
        return ParseStepsFromJson(text.Trim());
    }

    public async IAsyncEnumerable<string> StreamRoadmapFromGeminiAsync(string assignmentTitle, string? documentText, DateTime deadline, string routineSummary, string? assignmentDescription = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var apiKey = _config["Gemini:ApiKey"] ?? _config["GoogleGemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) yield break;

        var doc = string.IsNullOrWhiteSpace(documentText) ? "No document provided." : documentText.Length > 6000 ? documentText.Substring(0, 6000) + "..." : documentText;
        var desc = string.IsNullOrWhiteSpace(assignmentDescription) ? "" : $"Assignment description: {assignmentDescription}\n\n";
        var today = DateTime.Today;
        var daysTotal = Math.Max(1, (deadline.Date - today).Days);
        var minSteps = Math.Max(4, Math.Min(8, daysTotal * 2));
        var maxSteps = Math.Min(25, Math.Max(8, daysTotal * 4));
        var prompt = $@"You are a study planner. The student must complete this assignment by the deadline. Create a DETAILED day-by-day roadmap.

Assignment title: {assignmentTitle}
Deadline (must complete by): {deadline:yyyy-MM-dd}
Today: {today:yyyy-MM-dd}
There are {daysTotal} days until the deadline. Spread the work across Day 1, Day 2, ... Day {daysTotal}. Use between {minSteps} and {maxSteps} steps total (about 2-4 steps per day).
IMPORTANT – ROUTINE = BUSY (do not suggest study during these times). Only suggest FREE time. Routine (BUSY): {routineSummary}

{desc}Assignment document/content:
{doc}

Return ONLY a valid JSON array. No other text, no markdown. Each step must have:
- step_title: Start with ""Day N"" and optional ""Morning"" or ""Evening"" and a short action (e.g. ""Day 1 – Evening: Set up project and database"")
- step_detail: RICH and DETAILED. Include: (1) exact technical steps and sub-tasks, (2) tools/commands if relevant, (3) what to deliver or check, (4) suggested duration. Write 2-5 sentences.
- suggested_date: YYYY-MM-DD (spread from {today:yyyy-MM-dd} to {deadline:yyyy-MM-dd})
- suggested_time: HH:mm start (24h, e.g. 08:00 or 18:30)
- suggested_end_time: HH:mm end (24h)

Return {minSteps} to {maxSteps} steps. Every step must have step_detail and suggested_end_time.";

        var model = GeminiModel;
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:streamGenerateContent";
        var body = new { contents = new[] { new { parts = new[] { new { text = prompt } } } }, generationConfig = new { temperature = 0.3f } };
        var json = JsonSerializer.Serialize(body);
        var client = GetClient();
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        SetGeminiAuth(request, apiKey);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Gemini streamGenerateContent failed: {Status} {Body}", response.StatusCode, errBody.Length > 400 ? errBody.Substring(0, 400) + "..." : errBody);
            yield break;
        }
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;
            // SSE format: "data: {...}" or plain NDJSON line
            var data = trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ? (trimmed.Length > 5 ? trimmed.Substring(5).Trim() : "") : trimmed;
            if (string.IsNullOrEmpty(data) || data == "[DONE]" || data == "{}") continue;
            string? text = null;
            try
            {
                using var doc2 = JsonDocument.Parse(data);
                var root = doc2.RootElement;
                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var c0 = candidates[0];
                    if (c0.TryGetProperty("content", out var content) && content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                    {
                        if (parts[0].TryGetProperty("text", out var textEl))
                            text = textEl.GetString();
                    }
                }
            }
            catch { /* skip malformed chunk */ }
            if (!string.IsNullOrEmpty(text)) yield return text;
        }
    }

    public static List<RoadmapStepDto>? ParseStepsFromJson(string text)
    {
        text = text.Trim();
        var codeBlock = Regex.Match(text, @"```(?:json)?\s*([\s\S]*?)```");
        if (codeBlock.Success) text = codeBlock.Groups[1].Value.Trim();
        var start = text.IndexOf('[');
        var end = text.LastIndexOf(']');
        if (start < 0 || end < 0 || end <= start) return null;
        text = text.Substring(start, end - start + 1);

        // Normalize so both snake_case (step_title) and camelCase (stepTitle) from AI work
        text = Regex.Replace(text, @"""stepTitle""", "\"step_title\"", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"""stepDetail""", "\"step_detail\"", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"""suggestedDate""", "\"suggested_date\"", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"""suggestedTime""", "\"suggested_time\"", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"""suggestedEndTime""", "\"suggested_end_time\"", RegexOptions.IgnoreCase);

        try
        {
            var list = JsonSerializer.Deserialize<List<RoadmapStepDto>>(text);
            if (list == null || list.Count == 0) return null;
            foreach (var s in list)
            {
                if (string.IsNullOrWhiteSpace(s.step_title)) s.step_title = "Complete this step";
                if (string.IsNullOrWhiteSpace(s.step_detail)) s.step_detail = "Follow the step title and complete in the given time.";
            }
            return list;
        }
        catch { return null; }
    }
}
