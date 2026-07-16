using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace ServerlessAPI.Dtos;

public record GenerateCoordinatorReportRequest
{
    [Required]
    [RegularExpression(
        "^(Attendance|Grades|Users)$",
        ErrorMessage = "ReportType must be Attendance, Grades or Users.")]
    public string ReportType { get; init; } = string.Empty;

    public DateOnly? StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    [Range(1, int.MaxValue)]
    public int? CourseId { get; init; }

    [StringLength(50)]
    public string? Term { get; init; }

    [StringLength(20)]
    public string? GradeLevel { get; init; }

    [StringLength(20)]
    public string? Section { get; init; }

    [RegularExpression(
        "^(Student|Teacher|Coordinator)$",
        ErrorMessage = "UserRole must be Student, Teacher or Coordinator.")]
    public string? UserRole { get; init; }

    public bool? IsActive { get; init; }
}

public record CoordinatorReportSummaryResponse(
    int Id,
    string ReportType,
    int GeneratedByUserId,
    DateTime GeneratedAt);

public record CoordinatorReportResponse(
    int Id,
    string ReportType,
    JsonElement Filters,
    JsonElement Result,
    int GeneratedByUserId,
    DateTime GeneratedAt);

public record AttendanceCoordinatorReportRow(
    int CourseId,
    string CourseName,
    string GradeLevel,
    string Section,
    DateOnly Date,
    int PresentCount,
    int AbsentCount,
    int TotalCount);

public record GradesCoordinatorReportRow(
    int CourseId,
    string CourseName,
    string GradeLevel,
    string Section,
    string Term,
    int StudentCount,
    decimal AverageGrade,
    int PassedCount,
    int FailedCount);

public record UsersCoordinatorReportRow(
    string Role,
    int ActiveCount,
    int InactiveCount,
    int TotalCount);
