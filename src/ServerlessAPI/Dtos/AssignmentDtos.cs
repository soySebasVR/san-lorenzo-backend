using System.ComponentModel.DataAnnotations;

namespace ServerlessAPI.Dtos;

public record CreateAssignmentRequest
{
    [Range(1, int.MaxValue)]
    public int CourseId { get; init; }

    [Required, StringLength(150, MinimumLength = 1)]
    public string Title { get; init; } = string.Empty;

    /// <summary>"Task" o "Exam".</summary>
    [Required]
    public string Type { get; init; } = string.Empty;

    [Required]
    public DateOnly StartDate { get; init; }

    [Required]
    public DateOnly DueDate { get; init; }
}

public record TeacherAssignment(
    int Id,
    int CourseId,
    string CourseName,
    string Title,
    string Type,
    DateOnly StartDate,
    DateOnly DueDate);

/// <summary>El estado depende de la fecha de entrega.</summary>
public record StudentAssignment(
    int Id,
    string CourseName,
    string Title,
    string Type,
    DateOnly StartDate,
    DateOnly DueDate,
    string Status);
