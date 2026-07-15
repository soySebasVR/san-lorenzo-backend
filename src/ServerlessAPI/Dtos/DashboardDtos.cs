namespace ServerlessAPI.Dtos;

public record CourseSummary(
    int Id,
    string Name,
    string GradeLevel,
    string Section,
    string? Schedule);

public record DashboardResponse(
    int TotalCourses,
    int TotalStudents,
    int Pending,
    IReadOnlyList<CourseSummary> Courses);
