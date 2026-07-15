namespace ServerlessAPI.Entities;

public class Teacher
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>Job title shown in the profile ("Docente", "Jefe de área"…).</summary>
    public string Position { get; set; } = string.Empty;

    public string? Subjects { get; set; }
    public bool EmailNotifications { get; set; }
    public bool AppNotifications { get; set; }

    public ICollection<Course> Courses { get; set; } = [];
    public ICollection<Announcement> Announcements { get; set; } = [];
}
