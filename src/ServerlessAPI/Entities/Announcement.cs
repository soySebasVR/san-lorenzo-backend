namespace ServerlessAPI.Entities;

public class Announcement
{
    public int Id { get; set; }
    public int TeacherId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    // Filtros opcionales (null ignora el filtro).
    public string? Section { get; set; }
    public string? GradeLevel { get; set; }
    public int? CourseId { get; set; }

    public int RecipientCount { get; set; }
    public DateTime SentAt { get; set; }

    public Teacher Teacher { get; set; } = null!;
    public Course? Course { get; set; }
}
