namespace AssignmentManagement.Api.Models;

public class RoutineSlot
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int DayOfWeek { get; set; } // 0=Sunday .. 6=Saturday
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateRoutineSlotRequest
{
    public int DayOfWeek { get; set; }
    public string StartTime { get; set; } = ""; // "09:00"
    public string EndTime { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
}

public class UpdateRoutineSlotRequest : CreateRoutineSlotRequest { }
