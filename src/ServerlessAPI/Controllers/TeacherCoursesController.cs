using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerlessAPI.Authentication;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Repositories;

namespace ServerlessAPI.Controllers;

[ApiController]
[ApiExplorerSettings(GroupName = "Teacher")]
[Authorize(Roles = nameof(Role.Teacher))]
[Route("docente/cursos")]
[Produces("application/json")]
public class TeacherCoursesController(
    ILogger<TeacherCoursesController> logger,
    IUserContext userContext,
    ICourseRepository repository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CourseResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CourseResponse>>> Get(
        [FromQuery] string? section,
        [FromQuery] string? gradeLevel,
        [FromQuery] string? name,
        CancellationToken ct)
    {
        var teacherId = userContext.TeacherId;
        var courses = await repository.GetCoursesAsync(teacherId, section, gradeLevel, name, ct);

        logger.LogInformation("Listed {Total} courses for teacher {TeacherId}", courses.Count, teacherId);

        return Ok(courses);
    }

    [HttpGet("{courseId:int}")]
    [ProducesResponseType<CourseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseResponse>> GetById(int courseId, CancellationToken ct)
    {
        var course = await repository.GetCourseAsync(courseId, userContext.TeacherId, ct);

        return course is null
            ? NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not found",
                Detail = $"El curso {courseId} no existe o no pertenece a este docente.",
            })
            : Ok(course);
    }
}
