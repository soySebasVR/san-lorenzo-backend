using System.ComponentModel.DataAnnotations;

namespace ServerlessAPI.Dtos;

// ── Dashboard (/alumno/inicio) ───────────────────────────────────────────────
public record StudentDashboardResponse(
    decimal OverallAverage,
    int AttendancePercentage,
    int TotalCourses,
    IReadOnlyList<StudentCourseGrade> Courses);

// ── Courses / grades (/alumno/cursos, /alumno/notas) ─────────────────────────
public record StudentCourse(
    int Id,
    string Name,
    string TeacherName,
    string Color);

public record StudentCourseGrade(
    int CourseId,
    string Name,
    string TeacherName,
    decimal Average);

public record StudentGradesResponse(
    string GradeLevel,
    string Section,
    string Term,
    IReadOnlyList<StudentCourseGrade> Courses);

// ── Attendance (/alumno/asistencia) ──────────────────────────────────────────
public record StudentAttendanceItem(
    DateOnly Date,
    string Course,
    bool Present);

public record StudentAttendanceResponse(
    int TotalAbsences,
    IReadOnlyList<StudentAttendanceItem> History);

// ── Schedule (/alumno/horarios) ──────────────────────────────────────────────
public record StudentScheduleResponse(
    string GradeLevel,
    string Section,
    IReadOnlyList<ScheduledClass> Classes);

// ── Profile (/alumno/perfil) ─────────────────────────────────────────────────
public record StudentProfileResponse(
    int StudentId,
    string FullName,
    string GradeLevel,
    string Section,
    string? Email,
    string? Phone,
    bool EmailNotifications,
    bool AppNotifications);

/// <summary>Name, grade and section are institutional and not editable here.</summary>
public record UpdateStudentProfileRequest
{
    [EmailAddress]
    [StringLength(255)]
    public string? Email { get; init; }

    [StringLength(30)]
    public string? Phone { get; init; }

    public bool EmailNotifications { get; init; }
    public bool AppNotifications { get; init; }
}
