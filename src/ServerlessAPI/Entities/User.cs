namespace ServerlessAPI.Entities;

/// <summary>
/// Login credential. Kept apart from Teacher/Student, which are academic records:
/// an account can be deactivated without deleting someone's history.
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Role Role { get; set; }
    public bool IsActive { get; set; } = true;

    // Notification preferences. Teachers/students keep their own on their profile; for
    // coordinators (who have no academic profile) this is where they live.
    public bool EmailNotifications { get; set; }
    public bool AppNotifications { get; set; }

    // Which profile this account maps to, depending on Role. Coordinators have neither.
    public int? TeacherId { get; set; }
    public int? StudentId { get; set; }

    public Teacher? Teacher { get; set; }
    public Student? Student { get; set; }
}
