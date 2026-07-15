using System.ComponentModel.DataAnnotations;

namespace ServerlessAPI.Dtos;

public record StudentAttendance(
    int StudentId,
    string Name,
    bool Present);

public record AttendanceResponse(
    int CourseId,
    string Course,
    DateOnly Date,
    IReadOnlyList<StudentAttendance> Students);

public record AttendanceEntry
{
    [Range(1, int.MaxValue)]
    public int StudentId { get; init; }

    public bool Present { get; init; }
}

public record SaveAttendanceRequest
{
    [Range(1, int.MaxValue)]
    public int CourseId { get; init; }

    [Required]
    public DateOnly Date { get; init; }

    [Required]
    [MinLength(1)]
    public IReadOnlyList<AttendanceEntry> Entries { get; init; } = [];
}
