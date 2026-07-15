using System.ComponentModel.DataAnnotations;

namespace ServerlessAPI.Dtos;

public record StudentGrade(
    int StudentId,
    string Name,
    decimal Score1,
    decimal Score2,
    decimal Score3,
    decimal Score4,
    decimal Score5,
    decimal Average);

public record GradesResponse(
    string Course,
    string GradeLevel,
    string Section,
    string Term,
    IReadOnlyList<StudentGrade> Entries);

public record UpdateGradeRequest
{
    [Range(1, int.MaxValue)]
    public int CourseId { get; init; }

    [Required]
    [StringLength(20, MinimumLength = 1)]
    public string Term { get; init; } = string.Empty;

    // 0-20 scale.
    [Range(0, 20)] public decimal Score1 { get; init; }
    [Range(0, 20)] public decimal Score2 { get; init; }
    [Range(0, 20)] public decimal Score3 { get; init; }
    [Range(0, 20)] public decimal Score4 { get; init; }
    [Range(0, 20)] public decimal Score5 { get; init; }
}
