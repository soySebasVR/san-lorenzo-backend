using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Repositories;

namespace ServerlessAPI.Controllers;

/// <summary>Academic management (Gestión Académica). Courses, plus lookups for the forms.</summary>
[ApiController]
[ApiExplorerSettings(GroupName = "Coordinator")]
[Authorize(Roles = nameof(Role.Coordinator))]
[Route("coordinador")]
[Produces("application/json")]
public class CoordinatorAcademicController(
    ILogger<CoordinatorAcademicController> logger,
    ICourseAdminRepository repository) : ControllerBase
{
    [HttpGet("cursos")]
    [ProducesResponseType<IReadOnlyList<AdminCourse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminCourse>>> ListCourses(CancellationToken ct) =>
        Ok(await repository.ListAsync(ct));

    [HttpGet("cursos/{id:int}")]
    [ProducesResponseType<AdminCourse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminCourse>> GetCourse(int id, CancellationToken ct)
    {
        var course = await repository.GetAsync(id, ct);
        return course is null ? NotFound() : Ok(course);
    }

    [HttpPost("cursos")]
    [ProducesResponseType<AdminCourse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminCourse>> CreateCourse(
        [FromBody] SaveCourseRequest request,
        CancellationToken ct)
    {
        var course = await repository.CreateAsync(request, ct);
        logger.LogInformation("Course created. Id {CourseId}, {Name} {Grade}-{Section}",
            course.Id, course.Name, course.GradeLevel, course.Section);
        return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, course);
    }

    [HttpPut("cursos/{id:int}")]
    [ProducesResponseType<AdminCourse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminCourse>> UpdateCourse(
        int id,
        [FromBody] SaveCourseRequest request,
        CancellationToken ct)
    {
        var course = await repository.UpdateAsync(id, request, ct);
        return course is null ? NotFound() : Ok(course);
    }

    [HttpDelete("cursos/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCourse(int id, CancellationToken ct)
    {
        var deleted = await repository.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Teachers, for the course-assignment dropdown.</summary>
    [HttpGet("docentes")]
    [ProducesResponseType<IReadOnlyList<TeacherOption>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TeacherOption>>> ListTeachers(CancellationToken ct) =>
        Ok(await repository.GetTeachersAsync(ct));

    /// <summary>Distinct grade + section combinations currently in use.</summary>
    [HttpGet("grados-secciones")]
    [ProducesResponseType<IReadOnlyList<GradeSection>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GradeSection>>> ListGradeSections(CancellationToken ct) =>
        Ok(await repository.GetGradeSectionsAsync(ct));
}
