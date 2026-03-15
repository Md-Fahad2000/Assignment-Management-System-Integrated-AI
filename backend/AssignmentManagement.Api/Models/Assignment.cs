namespace AssignmentManagement.Api.Models;

public class Assignment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public string Priority { get; set; } = "medium"; 
    public string Status { get; set; } = "pending"; 
    public string? DocumentPath { get; set; }
    public string? DocumentText { get; set; }
    /// <summary>.</summary>
    public string? RoadmapJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateAssignmentRequest
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string DueDate { get; set; } = ""; 
    public string Priority { get; set; } = "medium";
}

public class UpdateAssignmentRequest : CreateAssignmentRequest
{
    public string? Status { get; set; }
}
