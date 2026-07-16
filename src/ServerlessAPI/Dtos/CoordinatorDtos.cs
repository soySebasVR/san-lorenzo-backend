using System.ComponentModel.DataAnnotations;

namespace ServerlessAPI.Dtos;

// ── Inicio ──────────────────────────────────────────
public record CoordinatorDashboardResponse(
    int TotalStudents,
    int ActiveTeachers,
    int AttendanceTodayPercentage,
    int GradesCompliancePercentage);

// ── Usuarios ────────────────────────────────────────────
public record UserListItem(
    int Id,
    string FullName,
    string Email,
    string Role,
    bool IsActive);

public record UserDetail(
    int Id,
    string FullName,
    string Email,
    string Role,
    bool IsActive,
    int? TeacherId,
    int? StudentId);

public record CreateUserRequest
{
    [Required, EmailAddress, StringLength(255)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get; init; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;

    /// <summary>"Teacher", "Student" o "Coordinator".</summary>
    [Required]
    public string Role { get; init; } = string.Empty;

    // Id opcional según el rol del usuario.
    public int? TeacherId { get; init; }
    public int? StudentId { get; init; }
}

public record UpdateUserRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}

// ── Cursos ────────────────────────────────
public record AdminCourse(
    int Id,
    string Name,
    string GradeLevel,
    string Section,
    int TeacherId,
    string TeacherName,
    string Color);

public record SaveCourseRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [Required, StringLength(20)]
    public string GradeLevel { get; init; } = string.Empty;

    [Required, StringLength(20)]
    public string Section { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int TeacherId { get; init; }

    [Required, StringLength(20)]
    public string Color { get; init; } = string.Empty;
}

public record TeacherOption(int Id, string FullName);

public record GradeSection(string GradeLevel, string Section);

// ── Horarios ──────────────────────────────
public record AdminScheduleSlot(
    int Id,
    int CourseId,
    string CourseName,
    string GradeLevel,
    string Section,
    string StartTime,
    string EndTime,
    int DayOfWeek,
    string Icon);

public record SaveScheduleSlotRequest
{
    [Range(1, int.MaxValue)]
    public int CourseId { get; init; }

    /// <summary>"HH:mm".</summary>
    [Required, RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$")]
    public string StartTime { get; init; } = string.Empty;

    [Required, RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$")]
    public string EndTime { get; init; } = string.Empty;

    [Range(0, 6)]
    public int DayOfWeek { get; init; }

    [Required, StringLength(50)]
    public string Icon { get; init; } = string.Empty;
}

// ── Configuración ────────────────────────────────────
public record SettingsResponse(
    string SchoolName,
    int AcademicYear,
    string CurrentTerm,
    int UnjustifiedAbsenceThreshold,
    int LatenessToleranceMinutes);

public record UpdateSettingsRequest
{
    [Required, StringLength(150, MinimumLength = 1)]
    public string SchoolName { get; init; } = string.Empty;

    [Range(2000, 2100)]
    public int AcademicYear { get; init; }

    [Required, StringLength(20, MinimumLength = 1)]
    public string CurrentTerm { get; init; } = string.Empty;

    [Range(0, 100)]
    public int UnjustifiedAbsenceThreshold { get; init; }

    [Range(0, 120)]
    public int LatenessToleranceMinutes { get; init; }
}

// ── Perfil ────────────────────────────────
public record CoordinatorProfileResponse(
    int UserId,
    string FullName,
    string Email,
    bool EmailNotifications,
    bool AppNotifications);

public record UpdateCoordinatorProfileRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(255)]
    public string Email { get; init; } = string.Empty;

    public bool EmailNotifications { get; init; }
    public bool AppNotifications { get; init; }
}
