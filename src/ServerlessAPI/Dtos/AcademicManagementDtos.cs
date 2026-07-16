using System.ComponentModel.DataAnnotations;

namespace ServerlessAPI.Dtos;

public record GradeResponse(int Id, string Name, string Level);

public record SectionResponse(int Id, int GradeId, string GradeName, string Name, int Capacity);

public record CoordinatorCourseResponse(
    int Id, string Name, string GradeName, string SectionName, string TeacherName, int WeeklyHours
);

public record CreateCourseRequest
{
    [Required] [MaxLength(150)] public string Name { get; init; } = string.Empty;
    [Required] public int GradeId { get; init; }
    [Required] public int SectionId { get; init; }
    [Required] public int TeacherId { get; init; }
    public int WeeklyHours { get; init; }
}

public record UpdateCourseRequest : CreateCourseRequest;