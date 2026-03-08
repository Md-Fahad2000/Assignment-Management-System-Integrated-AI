namespace AssignmentManagement.Api.Models;

public class Assignment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public string Priority { get; set; } = "medium"; // low, medium, high
    public string Status { get; set; } = "pending"; // pending, in_progress, completed
    public string? DocumentPath { get; set; }
    public string? DocumentText { get; set; }
    /// <summary>AI-generated roadmap as JSON array: [{ "step", "description", "startDate", "endDate" }].</summary>
    public string? RoadmapJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateAssignmentRequest
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string DueDate { get; set; } = ""; // YYYY-MM-DD
    public string Priority { get; set; } = "medium";
}

public class UpdateAssignmentRequest : CreateAssignmentRequest
{
    public string? Status { get; set; }
}
