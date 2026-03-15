using System.Text;
using System.Text.Json;
using AssignmentManagement.Api.Models;

namespace AssignmentManagement.Api.Services;

public interface IAssignmentExtraService
{
    Task<string?> SummarizeAsync(Assignment assignment, CancellationToken cancellationToken = default);
    Task<string?> GetSuggestedSlotsAsync(Assignment assignment, string routineSummary, CancellationToken cancellationToken = default);
    Task<string?> SuggestTitleFromDocumentAsync(Assignment assignment, CancellationToken cancellationToken = default);
    Task<string?> GetKeyPointsAsync(Assignment assignment, CancellationToken cancellationToken = default);
    Task<string?> SimplifyRoadmapAsync(Assignment assignment, CancellationToken cancellationToken = default);
    Task<string?> GetFocusTipAsync(IEnumerable<Assignment> assignments, string routineSummary, CancellationToken cancellationToken = default);
    Task<string?> GetWeeklyPlanAsync(IEnumerable<Assignment> assignments, string routineSummary, CancellationToken cancellationToken = default);
    Task<string?> GetSuggestedSlotsWithReasonsAsync(Assignment assignment, string routineSummary, CancellationToken cancellationToken = default);
    Task<string?> GetSuggestedFollowUpsAsync(string lastAssistantReply, CancellationToken cancellationToken = default);
    Task<string?> GetKeyDatesFromDocumentAsync(Assignment assignment, CancellationToken cancellationToken = default);
    Task<string?> GetQuizFromAssignmentAsync(Assignment assignment, CancellationToken cancellationToken = default);
    Task<string?> GetOneLineSummaryAsync(Assignment assignment, CancellationToken cancellationToken = default);
    Task<string?> RescheduleRoadmapFromStepAsync(Assignment assignment, int fromStepIndex, string routineSummary, CancellationToken cancellationToken = default);
    Task<string?> GetOverloadSuggestionAsync(IEnumerable<Assignment> assignments, string overloadMessage, CancellationToken cancellationToken = default);
    Task<string?> GetBestDayAsync(Assignment assignment, string routineSummary, CancellationToken cancellationToken = default);
    Task<string?> GetConflictAlternativeAsync(Assignment assignment, int stepIndex, string conflictWith, string routineSummary, CancellationToken cancellationToken = default);
    Task<string?> ExplainStepInPlaceAsync(Assignment assignment, string stepTitle, string? stepDescription, CancellationToken cancellationToken = default);
    Task<string?> GetDifficultyAndTimePerStepAsync(Assignment assignment, CancellationToken cancellationToken = default);
    /// <summary>.</summary>
    Task<string?> GetStressEstimateAsync(IEnumerable<Assignment> assignments, string routineSummary, CancellationToken cancellationToken = default);
}

/// <summary>.</summary>
public class AssignmentExtraService : IAssignmentExtraService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<AssignmentExtraService> _logger;

    public AssignmentExtraService(IConfiguration config, IHttpClientFactory httpFactory, ILogger<AssignmentExtraService> logger)
    {
        _config = config;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public async Task<string?> SummarizeAsync(Assignment assignment, CancellationToken cancellationToken = default)
    {
        var doc = string.IsNullOrWhiteSpace(assignment.DocumentText)
            ? (assignment.Description ?? "No content.")
            : (assignment.DocumentText.Length > 6000 ? assignment.DocumentText.Substring(0, 6000) + "..." : assignment.DocumentText);
        var system = "You are a helpful assistant. Summarize the given assignment document in 2-4 short paragraphs. Be concise and highlight key requirements and deadlines if mentioned.";
        var user = $"Assignment title: {assignment.Title}\n\nContent:\n{doc}";
        return await CallLlmAsync(system, user, cancellationToken);
    }

    public async Task<string?> GetSuggestedSlotsAsync(Assignment assignment, string routineSummary, CancellationToken cancellationToken = default)
    {
        var system = "You are a study planner. Given the student's routine (busy times) and assignment deadline, suggest 3-5 specific time windows when they could work on this assignment. Exclude 10 AM - 5 PM (job). Output only a list, one line per slot, e.g. 'Monday 7:00 PM - 9:00 PM' or 'Saturday 9 AM - 11 AM'. No numbering or extra text.";
        var user = $"Deadline: {assignment.DueDate:yyyy-MM-dd}. Routine (busy): {routineSummary}.";
        return await CallLlmAsync(system, user, cancellationToken);
    }

    public async Task<string?> SuggestTitleFromDocumentAsync(Assignment assignment, CancellationToken cancellationToken = default)
    {
        var doc = assignment.DocumentText ?? assignment.Description ?? "";
        if (string.IsNullOrWhiteSpace(doc)) return null;
        var content = doc.Length > 4000 ? doc.Substring(0, 4000) + "..." : doc;
        var system = "You are a helper. Based ONLY on the following document content, suggest a short assignment title (3-8 words). Output only the title, no quotes or explanation.";
        var user = content;
        return await CallLlmAsync(system, user, cancellationToken);
    }

    public async Task<string?> GetKeyPointsAsync(Assignment assignment, CancellationToken cancellationToken = default)
    {
        var doc = string.IsNullOrWhiteSpace(assignment.DocumentText) ? (assignment.Description ?? "No content.") : assignment.DocumentText;
        var content = doc.Length > 6000 ? doc.Substring(0, 6000) + "..." : doc;
        var system = "You are a helpful assistant. Extract 5-10 key points or bullet points from the assignment. Output one point per line, no numbering or bullets. Be concise.";
        var user = $"Assignment: {assignment.Title}\n\nContent:\n{content}";
        return await CallLlmAsync(system, user, cancellationToken);
    }

    public async Task<string?> SimplifyRoadmapAsync(Assignment assignment, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(assignment.RoadmapJson)) return null;
        var system = "You are a study planner. The user has an AI-generated roadmap (JSON array of steps with step, description, startDate, endDate). Return a SIMPLIFIED version: merge steps into roughly half the count, keeping the same deadline and coverage. Output ONLY a valid JSON array of objects with keys: step, description, startDate, endDate. No markdown, no explanation. Preserve date range from first to last step.";
        var user = $"Deadline: {assignment.DueDate:yyyy-MM-dd}. Current roadmap:\n{assignment.RoadmapJson}";
        return await CallLlmAsync(system, user, cancellationToken);
    }

    public async Task<string?> GetFocusTipAsync(IEnumerable<Assignment> assignments, string routineSummary, CancellationToken cancellationToken = default)
    {
        var list = assignments.Where(a => a.Status != "completed" && !string.IsNullOrWhiteSpace(a.RoadmapJson)).Take(5).ToList();
        if (list.Count == 0) return null;
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var sb = new System.Text.StringBuilder();
        foreach (var a in list)
        {
            sb.AppendLine($"{a.Title} (due {a.DueDate:yyyy-MM-dd}); ");
        }
        var system = "You are a study coach. Given today's date and the list of assignments with roadmaps, output ONE short sentence (under 15 words) telling the student what to focus on today. E.g. 'Today: focus on Assignment X step 2 and 3.' Be specific and encouraging.";
        var user = $"Today: {today}. Assignments: {sb}. Routine (busy): {routineSummary}.";
        return await CallLlmAsync(system, user, cancellationToken);
    }

    public async Task<string?> GetWeeklyPlanAsync(IEnumerable<Assignment> assignments, string routineSummary, CancellationToken cancellationToken = default)
    {
        var list = assignments.Where(a => a.Status != "completed").ToList();
        if (list.Count == 0) return null;
        var sb = new System.Text.StringBuilder();
        foreach (var a in list)
        {
            sb.AppendLine($"- {a.Title} (due {a.DueDate:yyyy-MM-dd})");
            if (!string.IsNullOrWhiteSpace(a.RoadmapJson)) sb.AppendLine($"  Roadmap: {a.RoadmapJson.Length} chars");
        }
        var system = "You are a study planner. Summarize the student's week in one short paragraph (2-4 sentences): what assignments they have, what to prioritize, and a simple day-by-day suggestion. Use the assignment list and routine. Be concise and actionable.";
        var user = $"Assignments:\n{sb}\nRoutine (busy): {routineSummary}.";
        return await CallLlmAsync(system, user, cancellationToken);
    }

    public async Task<string?> GetSuggestedSlotsWithReasonsAsync(Assignment assignment, string routineSummary, CancellationToken cancellationToken = default)
    {
        var system = "You are a study planner. Given the student's routine (busy times) and assignment deadline, suggest 3-5 specific time windows when they could work on this assignment. Exclude 10 AM - 5 PM (job). For each slot output one line in this format: 'TIME_WINDOW - SHORT_REASON'. E.g. 'Monday 7:00 PM - 9:00 PM - 2 free hours after class'. No numbering.";
        var user = $"Deadline: {assignment.DueDate:yyyy-MM-dd}. Routine (busy): {routineSummary}.";
        return await CallLlmAsync(system, user, cancellationToken);
    }

    public async Task<string?> GetSuggestedFollowUpsAsync(string lastAssistantReply, CancellationToken cancellationToken = default)
    {
        var system = "You are a study assistant. Given the assistant's last reply, suggest exactly 3 short follow-up questions the student might ask next. Output one question per line. No numbering or bullets. Each question under 10 words.";
        return await CallLlmAsync(system, lastAssistantReply.Length > 1500 ? lastAssistantReply.Substring(0, 1500) + "..." : lastAssistantReply, cancellationToken);
    }

    public async Task<string?> GetKeyDatesFromDocumentAsync(Assignment assignment, CancellationToken cancellationToken = default)
    {
        var doc = assignment.DocumentText ?? assignment.Description ?? "";
        if (string.IsNullOrWhiteSpace(doc)) return null;
        var content = doc.Length > 6000 ? doc.Substring(0, 6000) + "..." : doc;
        var system = "Extract all dates and deadlines mentioned in the text (e.g. 'due 15th March', 'submit by 2025-04-01'). Output one per line. If none found, output 'No dates found.'";
        return await CallLlmAsync(system, content, cancellationToken);
    }

    public async Task<string?> GetQuizFromAssignmentAsync(Assignment assignment, CancellationToken cancellationToken = default)
    {
        var doc = string.IsNullOrWhiteSpace(assignment.DocumentText) ? (assignment.Description ?? "") : assignment.DocumentText;
        if (string.IsNullOrWhiteSpace(doc)) return null;
        var content = doc.Length > 5000 ? doc.Substring(0, 5000) + "..." : doc;
        var system = "Create a short practice quiz from the assignment. Output 5-8 question-answer pairs. Format: Q1: question text\nA1: answer text\nQ2: ... No numbering outside this format.";
        var user = $"Assignment: {assignment.Title}\n\n{content}";
        return await CallLlmAsync(system, user, cancellationToken);
    }

    public async Task<string?> GetOneLineSummaryAsync(Assignment assignment, CancellationToken cancellationToken = default)
    {
        var doc = string.IsNullOrWhiteSpace(assignment.DocumentText) ? (assignment.Description ?? "No content.") : assignment.DocumentText;
        var content = doc.Length > 3000 ? doc.Substring(0, 3000) + "..." : doc;
        var system = "Summarize in exactly one short sentence (under 20 words). No preamble.";
        return await CallLlmAsync(system, $"Title: {assignment.Title}\n\n{content}", cancellationToken);
    }

    public async Task<string?> RescheduleRoadmapFromStepAsync(Assignment assignment, int fromStepIndex, string routineSummary, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(assignment.RoadmapJson)) return null;
        var system = "You are a study planner. The user has a roadmap (JSON array with step, description, startDate, endDate). They could not complete steps from index " + fromStepIndex + " (0-based). Generate a NEW roadmap that keeps the same deadline but reschedules from that step onward. Output ONLY a valid JSON array of objects with keys: step, description, startDate, endDate. Same number or fewer steps from that point. No markdown.";
        var user = $"Deadline: {assignment.DueDate:yyyy-MM-dd}. Routine: {routineSummary}. Current roadmap:\n{assignment.RoadmapJson}";
        return await CallLlmAsync(system, user, cancellationToken);
    }

    public async Task<string?> GetOverloadSuggestionAsync(IEnumerable<Assignment> assignments, string overloadMessage, CancellationToken cancellationToken = default)
    {
        var list = assignments.Where(a => a.Status != "completed").Take(5).Select(a => $"{a.Title} (due {a.DueDate:yyyy-MM-dd})").ToList();
        var system = "You are a study coach. The student has too many hours scheduled this week. Suggest ONE concrete action in one sentence (e.g. 'Move Assignment X to next week' or 'Extend the deadline for Y by 2 days'). Be brief.";
        var user = $"Assignments: " + string.Join("; ", list) + ". Context: " + overloadMessage;
        return await CallLlmAsync(system, user, cancellationToken);
    }

    public async Task<string?> GetBestDayAsync(Assignment assignment, string routineSummary, CancellationToken cancellationToken = default)
    {
        var system = "Given the student's routine (busy times) and assignment deadline, recommend the single best day of the week to work on this assignment. One short sentence with reason.";
        var user = $"Deadline: {assignment.DueDate:yyyy-MM-dd}. Routine: {routineSummary}.";
        return await CallLlmAsync(system, user, cancellationToken);
    }

    public async Task<string?> GetConflictAlternativeAsync(Assignment assignment, int stepIndex, string conflictWith, string routineSummary, CancellationToken cancellationToken = default)
    {
        var system = "The student's roadmap step " + stepIndex + " overlaps with: " + conflictWith + ". Suggest one alternative time window (day and time) when they could do this step instead. One short sentence. Exclude 10 AM - 5 PM.";
        var user = "Routine (busy): " + routineSummary + ". Deadline: " + assignment.DueDate.ToString("yyyy-MM-dd");
        return await CallLlmAsync(system, user, cancellationToken);
    }

    public async Task<string?> ExplainStepInPlaceAsync(Assignment assignment, string stepTitle, string? stepDescription, CancellationToken cancellationToken = default)
    {
        var desc = string.IsNullOrWhiteSpace(stepDescription) ? "" : "\nStep detail: " + stepDescription;
        var system = "Explain this assignment roadmap step in 2-3 simple sentences so a student knows exactly what to do. No jargon.";
        var user = "Assignment: " + assignment.Title + ". Step: " + stepTitle + desc;
        return await CallLlmAsync(system, user, cancellationToken);
    }

    public async Task<string?> GetDifficultyAndTimePerStepAsync(Assignment assignment, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(assignment.RoadmapJson)) return null;
        var system = "For each step in this roadmap JSON, add 'difficulty' (easy/medium/hard) and 'estimatedMinutes' (number). Output a valid JSON array with same order, each object: step, description, startDate, endDate, difficulty, estimatedMinutes. No other text.";
        var user = assignment.RoadmapJson;
        return await CallLlmAsync(system, user, cancellationToken);
    }

    public async Task<string?> GetStressEstimateAsync(IEnumerable<Assignment> assignments, string routineSummary, CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var list = assignments.ToList();
        var sb = new StringBuilder();
        foreach (var a in list)
        {
            var status = a.Status ?? "pending";
            var due = a.DueDate.ToString("yyyy-MM-dd");
            var road = string.IsNullOrWhiteSpace(a.RoadmapJson) ? "no roadmap" : "has roadmap";
            sb.AppendLine($"- {a.Title} | due {due} | {status} | {road}");
        }
        var system = "You are a student wellness assistant. Based ONLY on the student's assignments and routine (no other data), estimate their stress level from 1 (low) to 5 (high) for: (a) previous week, (b) current week, (c) predicted for next week. Consider: overdue assignments, how many due soon, workload from roadmaps, and how busy their routine is. Reply with ONLY a valid JSON object with these exact keys: previous_week (number 1-5), current_week (number 1-5), future_predicted (number 1-5), message (one short sentence tip, optional). No markdown, no code block.";
        var user = $"Today: {today}. Assignments:\n{sb}\nRoutine (busy times): {routineSummary}.";
        return await CallLlmAsync(system, user, cancellationToken);
    }

    private async Task<string?> CallLlmAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = userPrompt }
        };
        string? lastError = null;
        foreach (var provider in new[] { CallOpenAiAsync, CallOpenRouterAsync, CallGroqAsync })
        {
            try
            {
                var result = await provider(messages, cancellationToken);
                if (!string.IsNullOrWhiteSpace(result)) return result;
            }
            catch (Exception ex) { lastError = ex.Message; }
        }
        if (lastError != null) throw new InvalidOperationException(lastError);
        return null;
    }

    private async Task<string?> CallOpenAiAsync(List<object> messages, CancellationToken ct)
    {
        var key = _config["OpenAI:ApiKey"] ?? _config["Ai:OpenAiApiKey"];
        if (string.IsNullOrWhiteSpace(key)) return null;
        return await Call(messages, "https://api.openai.com/v1/chat/completions", key, _config["OpenAI:Model"] ?? "gpt-4o-mini", ct);
    }

    private async Task<string?> CallOpenRouterAsync(List<object> messages, CancellationToken ct)
    {
        var key = _config["OpenRouter:ApiKey"];
        if (string.IsNullOrWhiteSpace(key)) return null;
        return await Call(messages, "https://openrouter.ai/api/v1/chat/completions", key, _config["OpenRouter:Model"] ?? "openrouter/free", ct);
    }

    private async Task<string?> CallGroqAsync(List<object> messages, CancellationToken ct)
    {
        var key = _config["Groq:ApiKey"];
        if (string.IsNullOrWhiteSpace(key)) return null;
        return await Call(messages, "https://api.groq.com/openai/v1/chat/completions", key, _config["Groq:Model"] ?? "llama-3.1-8b-instant", ct);
    }

    private async Task<string?> Call(List<object> messages, string url, string apiKey, string model, CancellationToken ct)
    {
        var client = _httpFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(60);
        var body = new { model, messages, temperature = 0.3 };
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + apiKey);
        var response = await client.SendAsync(request, ct);
        var resJson = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) return null;
        using var doc = JsonDocument.Parse(resJson);
        var choices = doc.RootElement.GetProperty("choices");
        if (choices.GetArrayLength() == 0) return null;
        var content = choices[0].GetProperty("message").GetProperty("content").GetString();
        return string.IsNullOrWhiteSpace(content) ? null : content.Trim();
    }
}
