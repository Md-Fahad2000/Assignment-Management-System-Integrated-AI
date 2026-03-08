namespace AssignmentManagement.Api.Models;

public class AiScheduleSlot
{
    public int Id { get; set; }
    public int? AssignmentId { get; set; }
    public DateTime SlotDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? Notes { get; set; }
    public string? AssignmentTitle { get; set; }
}

public class RoadmapStep
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public int StepOrder { get; set; }
    public string StepTitle { get; set; } = "";
    /// <summary>How to do this step (e.g. "Read the PDF, list all questions, note word limits.")</summary>
    public string? StepDetail { get; set; }
    public DateTime? SuggestedDate { get; set; }
    public TimeSpan? SuggestedTime { get; set; }
    /// <summary>End time for this step so we show "9:00 AM – 11:00 AM".</summary>
    public TimeSpan? SuggestedEndTime { get; set; }
    public bool Completed { get; set; }
}
