namespace ServerlessAPI.Entities;

public class Course
{
    public int Id { get; set; }
    public int TeacherId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string GradeLevel { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;

    /// <summary>Human-readable schedule for the dashboard, e.g. "Lun y Mié 08:00-09:00".</summary>
    public string? ScheduleText { get; set; }

    public Teacher Teacher { get; set; } = null!;
    public ICollection<Student> Students { get; set; } = [];
    public ICollection<Grade> Grades { get; set; } = [];
    public ICollection<Attendance> AttendanceRecords { get; set; } = [];
    public ICollection<ScheduleSlot> ScheduleSlots { get; set; } = [];
}
