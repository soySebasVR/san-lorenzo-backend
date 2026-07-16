using System.ComponentModel.DataAnnotations;

namespace ServerlessAPI.Dtos;

// ── Inicio ───────────────────────────────────────────────
public record StudentDashboardResponse(
    decimal OverallAverage,
    int AttendancePercentage,
    int TotalCourses,
    IReadOnlyList<StudentCourseGrade> Courses);

// ── Cursos y Notas ─────────────────────────
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

// ── Asistencia ──────────────────────────────────────────
public record StudentAttendanceItem(
    DateOnly Date,
    string Course,
    bool Present);

public record StudentAttendanceResponse(
    int TotalAbsences,
    IReadOnlyList<StudentAttendanceItem> History);

// ── Horarios ──────────────────────────────────────────────
public record StudentScheduleResponse(
    string GradeLevel,
    string Section,
    IReadOnlyList<ScheduledClass> Classes);

// ── Perfil ─────────────────────────────────────────────────
public record StudentProfileResponse(
    int StudentId,
    string FullName,
    string GradeLevel,
    string Section,
    string? Email,
    string? Phone,
    bool EmailNotifications,
    bool AppNotifications);

/// <summary>Nombre, grado y sección no editables aquí.</summary>
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
