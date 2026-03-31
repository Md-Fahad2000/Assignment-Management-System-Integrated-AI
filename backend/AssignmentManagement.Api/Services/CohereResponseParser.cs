using System.Text;
using System.Text.Json;

namespace AssignmentManagement.Api.Services;

internal static class CohereResponseParser
{
    /// <summary>Reads assistant text from Cohere API v2 <c>/v2/chat</c> JSON.</summary>
    public static string? ExtractAssistantText(string resJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(resJson);
            if (!doc.RootElement.TryGetProperty("message", out var message)) return null;
            if (!message.TryGetProperty("content", out var content)) return null;

            if (content.ValueKind == JsonValueKind.String)
                return content.GetString();

            if (content.ValueKind == JsonValueKind.Array)
            {
                var sb = new StringBuilder();
                foreach (var part in content.EnumerateArray())
                {
                    if (part.TryGetProperty("type", out var typeEl) && typeEl.GetString() == "text" &&
                        part.TryGetProperty("text", out var textEl))
                        sb.Append(textEl.GetString());
                }
                var s = sb.ToString();
                return string.IsNullOrWhiteSpace(s) ? null : s;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
