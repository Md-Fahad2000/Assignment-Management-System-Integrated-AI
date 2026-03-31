namespace AssignmentManagement.Api.Models;

public class AssignmentAssistantMessage
{
    public int Id { get; set; }
    public string Role { get; set; } = "user"; 
    public string Content { get; set; } = "";
    public int? Rating { get; set; } 
}

public class AssignmentAssistantRequest
{
    /// <summary>.</summary>
    public string? Message { get; set; }
    /// <summary>.</summary>
    public List<AssignmentAssistantMessage> Messages { get; set; } = new();
}

/// <summary>.</summary>
public class RoadmapStepJson
{
    public string? step { get; set; }
    public string? Step { get; set; }
    public string? startDate { get; set; }
    public string? StartDate { get; set; }
    public string? endDate { get; set; }
    public string? EndDate { get; set; }
}

public class SetRoadmapRequest
{
    [System.Text.Json.Serialization.JsonPropertyName("roadmap_json")]
    public string? RoadmapJson { get; set; }
}

public class SuggestFollowUpsRequest
{
    public string? LastReply { get; set; }
}

public class RescheduleRoadmapRequest
{
    public int FromStepIndex { get; set; }
}

public class ConflictAlternativeRequest
{
    public int StepIndex { get; set; }
    public string? ConflictWith { get; set; }
}

public class RateMessageRequest
{
    public int? Rating { get; set; } 
}

