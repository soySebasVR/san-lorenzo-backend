namespace ServerlessAPI.Dtos;

/// <summary>Formatos "HH:mm" para uso directo en el frontend.</summary>
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
