using System.ComponentModel.DataAnnotations;

namespace ServerlessAPI.Dtos;

public record GenerateReportRequest
{
    [Required, StringLength(20)]
    public string GradeLevel { get; init; } = string.Empty;

    [Required, StringLength(20)]
    public string Term { get; init; } = string.Empty;

    /// <summary>Opcional: restringir al curso de un docente.</summary>
    public int? TeacherId { get; init; }
}

public record ReportListItem(
    int Id,
    string GradeLevel,
    string Term,
    string? TeacherName,
    DateTime GeneratedAt);
