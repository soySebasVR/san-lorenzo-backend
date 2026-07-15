namespace ServerlessAPI.Dtos;

/// <summary>Times are "HH:mm" so the frontend can drop them straight into &lt;time datetime&gt;.</summary>
public record ScheduledClass(
    int Id,
    string Name,
    string GradeLevel,
    string Section,
    string StartTime,
    string EndTime,
    int DayOfWeek,
    string Icon);

public record ScheduleResponse(
    int TeacherId,
    IReadOnlyList<ScheduledClass> Classes);
