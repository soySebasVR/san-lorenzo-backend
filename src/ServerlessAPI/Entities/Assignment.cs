namespace ServerlessAPI.Entities;

public enum AssignmentType
{
    Task,
    Exam,
}

/// <summary>
/// A task or exam a teacher sets on a course. Per-student submission/completion is not
/// tracked yet; students see it filtered by due date.
/// </summary>
public class Assignment
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public AssignmentType Type { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly DueDate { get; set; }

    public Course Course { get; set; } = null!;
}
