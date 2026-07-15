namespace ServerlessAPI.Entities;

public class Student
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string Name { get; set; } = string.Empty;

    public Course Course { get; set; } = null!;
    public ICollection<Grade> Grades { get; set; } = [];
    public ICollection<Attendance> AttendanceRecords { get; set; } = [];
}
