namespace ServerlessAPI.Entities;

/// <summary>(StudentId, CourseId, Date) is the upsert key.</summary>
public class Attendance
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public DateOnly Date { get; set; }
    public bool Present { get; set; }

    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
}
