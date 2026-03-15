using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AssignmentManagement.Api.Services;

/// <summary></summary>
public interface IAssignmentRoadmapGeminiService
{
    /// <summary>.</summary>
    Task<string?> GenerateRoadmapJsonAsync(string pdfText, string routineSummary, DateTime deadline, CancellationToken cancellationToken = default);
}

public class AssignmentRoadmapGeminiService : IAssignmentRoadmapGeminiService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<AssignmentRoadmapGeminiService> _logger;

    public AssignmentRoadmapGeminiService(IConfiguration config, IHttpClientFactory httpFactory, ILogger<AssignmentRoadmapGeminiService> logger)
    {
        _config = config;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public async Task<string?> GenerateRoadmapJsonAsync(string pdfText, string routineSummary, DateTime deadline, CancellationToken cancellationToken = default)
    {
        var apiKey = _config["Gemini:ApiKey"] ?? _config["GoogleGemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Gemini API key is not set. Add Gemini:ApiKey in appsettings.json.");

        var assignmentText = string.IsNullOrWhiteSpace(pdfText) ? "(No assignment text provided. Upload a PDF or add a description.)" : (pdfText.Length > 8000 ? pdfText.Substring(0, 8000) + "..." : pdfText);
        var today = DateTime.Today;
        var deadlineStr = deadline.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var deadlineWords = deadline.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
        var todayStr = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var todayWords = today.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);

        var systemPrompt = $@"You are an academic strategist. Based on this Assignment Text: {assignmentText}, and the Student's Daily Routine: {routineSummary}, generate a step-by-step roadmap.

CRITICAL DATES (use these exactly):
- Today is {todayWords} (date format: {todayStr}).
- The assignment DEADLINE is {deadlineWords} (date format: {deadlineStr}). The student must finish by this date.
- Every step's startDate and endDate MUST be between today ({todayStr}) and the deadline ({deadlineStr}) only. Do NOT use November or any month/year after the deadline. Spread steps from {todayStr} to {deadlineStr}.

Strictly exclude working hours (10 AM - 5 PM) as the user is at a job. Output MUST be a raw JSON array of objects only, no other text: [{{ ""step"": string, ""description"": string, ""startDate"": ""ISO8601"", ""endDate"": ""ISO8601"" }}]. Use ISO8601 dates in YYYY-MM-DD or full ISO format. Do not include markdown or code blocks.";

        var primaryModel = _config["Gemini:Model"] ?? _config["GoogleGemini:Model"] ?? "gemini-flash-latest";
        var modelsToTry = new List<string> { primaryModel };

        Exception? lastException = null;
        foreach (var model in modelsToTry)
        {
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var result = await CallGeminiAsync(model, systemPrompt, apiKey, cancellationToken);
                    if (result != null) return result;
                }
                catch (OperationCanceledException ex)
                {
                    lastException = new InvalidOperationException("Request timed out. Please try again.", ex);
                    if (attempt < 3)
                    {
                        var delayMs = attempt * 2000;
                        _logger.LogInformation("Gemini timeout, retrying in {Delay}ms (attempt {Attempt}/3).", delayMs, attempt);
                        try { await Task.Delay(delayMs, cancellationToken); } catch (OperationCanceledException) { throw lastException; }
                        continue;
                    }
                    throw (InvalidOperationException)lastException;
                }
                catch (InvalidOperationException ex)
                {
                    lastException = ex;
                    var msg = ex.Message;
                    var isNotFound = msg.Contains("NotFound", StringComparison.OrdinalIgnoreCase) || msg.Contains("404") || msg.Contains("not found", StringComparison.OrdinalIgnoreCase);
                    if (isNotFound)
                        break;
                    var isQuotaOr429 = msg.Contains("429") || msg.Contains("TooManyRequests", StringComparison.OrdinalIgnoreCase) || msg.Contains("quota", StringComparison.OrdinalIgnoreCase);
                    var isRetryable = isQuotaOr429 ||
                                      msg.Contains("ServiceUnavailable", StringComparison.OrdinalIgnoreCase) ||
                                      msg.Contains("503") ||
                                      msg.Contains("high demand", StringComparison.OrdinalIgnoreCase);
                    if (isRetryable && attempt < 3)
                    {
                        var delayMs = isQuotaOr429 ? (attempt * 30_000) : (attempt * 2000);
                        _logger.LogInformation("Gemini {Reason}, retrying in {Delay}s (attempt {Attempt}/3).", isQuotaOr429 ? "quota/429" : "busy", delayMs / 1000, attempt);
                        await Task.Delay(delayMs, cancellationToken);
                        continue;
                    }
                    if (isRetryable && attempt == 3)
                        break;
                    throw;
                }
            }
        }

        var lastMsg = lastException?.Message ?? "";
        var friendlyMessage = lastMsg.Contains("429", StringComparison.OrdinalIgnoreCase) || lastMsg.Contains("quota", StringComparison.OrdinalIgnoreCase)
            ? "Free tier quota reached (e.g. 20 requests/min). Please wait a minute and try again."
            : lastMsg.Contains("high demand", StringComparison.OrdinalIgnoreCase)
            ? "AI model is busy. Please click Generate again in a minute."
            : (lastMsg.Length > 0 ? lastMsg : "AI request failed. Please try again.");
        throw new InvalidOperationException(friendlyMessage);
    }

    private async Task<string?> CallGeminiAsync(string model, string systemPrompt, string apiKey, CancellationToken cancellationToken)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
        var body = new
        {
            contents = new[] { new { parts = new[] { new { text = systemPrompt } } } },
            generationConfig = new { temperature = 0.3f }
        };
        var json = JsonSerializer.Serialize(body);
        var client = _httpFactory.CreateClient("Gemini");
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        request.Headers.TryAddWithoutValidation("X-goog-api-key", apiKey);

        var response = await client.SendAsync(request, cancellationToken);
        var resJson = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Gemini generateContent failed: {Status} {Model} {Body}", response.StatusCode, model, resJson.Length > 400 ? resJson.Substring(0, 400) + "..." : resJson);
            throw new InvalidOperationException($"{response.StatusCode}: {TryGetError(resJson)}");
        }

        string? text = null;
        using (var doc = JsonDocument.Parse(resJson))
        {
            var candidates = doc.RootElement.GetProperty("candidates");
            if (candidates.GetArrayLength() == 0) throw new InvalidOperationException("Gemini returned no content. Try again.");
            var parts = candidates[0].GetProperty("content").GetProperty("parts");
            if (parts.GetArrayLength() == 0) throw new InvalidOperationException("Gemini returned empty parts.");
            text = parts[0].GetProperty("text").GetString();
        }

        if (string.IsNullOrWhiteSpace(text)) throw new InvalidOperationException("Gemini returned empty text.");
        text = text.Trim();
        var codeBlock = Regex.Match(text, @"```(?:json)?\s*([\s\S]*?)```");
        if (codeBlock.Success) text = codeBlock.Groups[1].Value.Trim();
        var start = text.IndexOf('[');
        var end = text.LastIndexOf(']');
        if (start >= 0 && end > start)
            text = text.Substring(start, end - start + 1);
        if (string.IsNullOrWhiteSpace(text) || text.Length < 2) throw new InvalidOperationException("Could not extract JSON array from Gemini response.");
        return text;
    }

    private static string TryGetError(string resJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(resJson);
            if (doc.RootElement.TryGetProperty("error", out var err) && err.TryGetProperty("message", out var msg))
                return msg.GetString() ?? (resJson.Length > 200 ? resJson.Substring(0, 200) : resJson);
        }
        catch { }
        return resJson.Length > 200 ? resJson.Substring(0, 200) : resJson;
    }
}
