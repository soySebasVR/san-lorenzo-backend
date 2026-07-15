namespace ServerlessAPI.Dtos;

public record CourseResponse(
    int Id,
    string Name,
    string GradeLevel,
    string Section,
    string Color);
