using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AssignmentManagement.Api.Services;

/// <summary>.</summary>
public interface IAssignmentRoadmapGroqService
{
    Task<string?> GenerateRoadmapJsonAsync(string pdfText, string routineSummary, DateTime deadline, CancellationToken cancellationToken = default);
}

public class AssignmentRoadmapGroqService : IAssignmentRoadmapGroqService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<AssignmentRoadmapGroqService> _logger;

    public AssignmentRoadmapGroqService(IConfiguration config, IHttpClientFactory httpFactory, ILogger<AssignmentRoadmapGroqService> logger)
    {
        _config = config;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public async Task<string?> GenerateRoadmapJsonAsync(string pdfText, string routineSummary, DateTime deadline, CancellationToken cancellationToken = default)
    {
        var apiKey = _config["Groq:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) return null;

        var assignmentText = string.IsNullOrWhiteSpace(pdfText) ? "(No assignment text provided. Upload a PDF or add a description.)" : (pdfText.Length > 8000 ? pdfText.Substring(0, 8000) + "..." : pdfText);
        var today = DateTime.Today;
        var deadlineStr = deadline.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var deadlineWords = deadline.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
        var todayStr = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var todayWords = today.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);

        var prompt = $@"You are an academic strategist. Based on this Assignment Text: {assignmentText}, and the Student's Daily Routine: {routineSummary}, generate a step-by-step roadmap.

CRITICAL DATES (use these exactly):
- Today is {todayWords} (date format: {todayStr}).
- The assignment DEADLINE is {deadlineWords} (date format: {deadlineStr}). The student must finish by this date.
- Every step's startDate and endDate MUST be between today ({todayStr}) and the deadline ({deadlineStr}) only. Do NOT use November or any month/year after the deadline. Spread steps from {todayStr} to {deadlineStr}.

Strictly exclude working hours (10 AM - 5 PM) as the user is at a job. Output MUST be a raw JSON array of objects only, no other text: [{{ ""step"": string, ""description"": string, ""startDate"": ""ISO8601"", ""endDate"": ""ISO8601"" }}]. Use ISO8601 dates in YYYY-MM-DD or full ISO format. Do not include markdown or code blocks.";

        var client = _httpFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(120);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
        request.Headers.Add("Authorization", "Bearer " + apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(new
        {
            model = _config["Groq:Model"] ?? "llama-3.1-8b-instant",
            messages = new[] { new { role = "user", content = prompt } },
            temperature = 0.3
        }), Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request, cancellationToken);
        var resJson = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Groq generate roadmap failed: {Status} {Body}", response.StatusCode, resJson.Length > 300 ? resJson.Substring(0, 300) + "..." : resJson);
            return null;
        }

        string? text = null;
        try
        {
            using var doc = JsonDocument.Parse(resJson);
            var choices = doc.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() == 0) return null;
            var first = choices.EnumerateArray().First();
            text = first.GetProperty("message").GetProperty("content").GetString();
        }
        catch { return null; }

        if (string.IsNullOrWhiteSpace(text)) return null;
        text = text.Trim();
        var codeBlock = Regex.Match(text, @"```(?:json)?\s*([\s\S]*?)```");
        if (codeBlock.Success) text = codeBlock.Groups[1].Value.Trim();
        var start = text.IndexOf('[');
        var end = text.LastIndexOf(']');
        if (start >= 0 && end > start)
            text = text.Substring(start, end - start + 1);
        if (string.IsNullOrWhiteSpace(text) || text.Length < 2) return null;
        return text;
    }
}
