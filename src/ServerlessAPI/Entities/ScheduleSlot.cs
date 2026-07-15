namespace ServerlessAPI.Entities;

public class ScheduleSlot
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    /// <summary>0 = Sunday … 6 = Saturday, matching Date.getDay() in the frontend.</summary>
    public int DayOfWeek { get; set; }

    public string Icon { get; set; } = string.Empty;

    public Course Course { get; set; } = null!;
}
