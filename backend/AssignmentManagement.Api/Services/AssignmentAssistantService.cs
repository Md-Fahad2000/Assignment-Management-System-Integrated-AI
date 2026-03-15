using System.Text;
using System.Text.Json;
using AssignmentManagement.Api.Models;

namespace AssignmentManagement.Api.Services;

public interface IAssignmentAssistantService
{
    Task<string?> AskAsync(Assignment assignment, string routineSummary, IReadOnlyList<AssignmentAssistantMessage> messages, CancellationToken cancellationToken = default);
}

/// <summary>
/// </summary>
public class AssignmentAssistantService : IAssignmentAssistantService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<AssignmentAssistantService> _logger;

    public AssignmentAssistantService(IConfiguration config, IHttpClientFactory httpFactory, ILogger<AssignmentAssistantService> logger)
    {
        _config = config;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public async Task<string?> AskAsync(Assignment assignment, string routineSummary, IReadOnlyList<AssignmentAssistantMessage> messages, CancellationToken cancellationToken = default)
    {
        var context = BuildSystemContext(assignment, routineSummary);
        var chatMessages = new List<object>
        {
            new { role = "system", content = context }
        };
        foreach (var m in messages)
        {
            if (string.IsNullOrWhiteSpace(m.Content)) continue;
            var role = string.IsNullOrWhiteSpace(m.Role) ? "user" : m.Role.ToLowerInvariant();
            if (role != "user" && role != "assistant") role = "user";
            chatMessages.Add(new { role, content = m.Content });
        }

        var providers = new List<Func<List<object>, CancellationToken, Task<string?>>>
        {
            CallOpenAiAsync,
            CallOpenRouterAsync,
            CallGroqAsync
        };

        string? lastError = null;
        foreach (var provider in providers)
        {
            try
            {
                var answer = await provider(chatMessages, cancellationToken);
                if (!string.IsNullOrWhiteSpace(answer)) return answer;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                _logger.LogWarning(ex, "Assignment assistant provider failed.");
            }
        }

        if (lastError != null) throw new InvalidOperationException(lastError);
        return null;
    }

    private string BuildSystemContext(Assignment a, string routineSummary)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an assignment assistant. You MUST base every answer ONLY on the following two things: (1) this student's DAILY ROUTINE, and (2) this ASSIGNMENT (title, description, document, deadline, roadmap). Do not invent details; use only what is provided below.");
        sb.AppendLine();
        sb.AppendLine("=== STUDENT'S DAILY ROUTINE (busy times – do not suggest work during these) ===");
        sb.AppendLine(string.IsNullOrWhiteSpace(routineSummary) ? "No fixed routine." : routineSummary);
        sb.AppendLine();
        sb.AppendLine("=== ASSIGNMENT (use this as the only assignment context) ===");
        sb.AppendLine($"Title: {a.Title}");
        sb.AppendLine($"Deadline: {a.DueDate:yyyy-MM-dd}");
        sb.AppendLine($"Priority: {a.Priority}");
        sb.AppendLine($"Status: {a.Status}");
        if (!string.IsNullOrWhiteSpace(a.Description))
            sb.AppendLine($"Description: {a.Description}");
        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(a.DocumentText))
        {
            var doc = a.DocumentText.Length > 8000 ? a.DocumentText.Substring(0, 8000) + "..." : a.DocumentText;
            sb.AppendLine("Assignment document / PDF text:");
            sb.AppendLine(doc);
            sb.AppendLine();
        }
        if (!string.IsNullOrWhiteSpace(a.RoadmapJson))
        {
            sb.AppendLine("AI-generated roadmap (steps with startDate/endDate):");
            sb.AppendLine(a.RoadmapJson);
            sb.AppendLine();
        }
        sb.AppendLine("=== RULES ===");
        sb.AppendLine("- Answer ONLY using the routine and assignment above. Do not suggest work between 10:00 and 17:00 (job hours).");
        sb.AppendLine("- Be concrete: what to do today/this week, how to approach the work, referencing the roadmap if present.");
        sb.AppendLine("- Respond in the same language as the user (Urdu or English).");
        return sb.ToString();
    }

    private async Task<string?> CallOpenAiAsync(List<object> messages, CancellationToken cancellationToken)
    {
        var apiKey = _config["OpenAI:ApiKey"] ?? _config["Ai:OpenAiApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) return null;
        var model = _config["OpenAI:Model"] ?? "gpt-4o-mini";
        return await CallOpenAiStyleEndpointAsync(
            "https://api.openai.com/v1/chat/completions",
            apiKey,
            model,
            messages,
            cancellationToken);
    }

    private async Task<string?> CallOpenRouterAsync(List<object> messages, CancellationToken cancellationToken)
    {
        var apiKey = _config["OpenRouter:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) return null;
        var model = _config["OpenRouter:Model"] ?? "openrouter/free";
        return await CallOpenAiStyleEndpointAsync(
            "https://openrouter.ai/api/v1/chat/completions",
            apiKey,
            model,
            messages,
            cancellationToken);
    }

    private async Task<string?> CallGroqAsync(List<object> messages, CancellationToken cancellationToken)
    {
        var apiKey = _config["Groq:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey)) return null;
        var model = _config["Groq:Model"] ?? "llama-3.1-8b-instant";
        return await CallOpenAiStyleEndpointAsync(
            "https://api.groq.com/openai/v1/chat/completions",
            apiKey,
            model,
            messages,
            cancellationToken);
    }

    private async Task<string?> CallOpenAiStyleEndpointAsync(string url, string apiKey, string model, List<object> messages, CancellationToken cancellationToken)
    {
        var client = _httpFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(120);

        var body = new
        {
            model,
            messages,
            temperature = 0.3
        };
        var json = JsonSerializer.Serialize(body);
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + apiKey);

        var response = await client.SendAsync(request, cancellationToken);
        var resJson = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Assignment assistant endpoint {Url} failed: {Status} {Body}", url, response.StatusCode, resJson.Length > 400 ? resJson.Substring(0, 400) + "..." : resJson);
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(resJson);
            var choices = doc.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() == 0) return null;
            var first = choices[0];
            var msg = first.GetProperty("message");
            var content = msg.GetProperty("content").GetString();
            return string.IsNullOrWhiteSpace(content) ? null : content.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse assistant response JSON.");
            return null;
        }
    }
}

