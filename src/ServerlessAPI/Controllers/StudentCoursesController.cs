using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerlessAPI.Authentication;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Repositories;

namespace ServerlessAPI.Controllers;

[ApiController]
[Authorize(Roles = nameof(Role.Student))]
[Route("alumno/cursos")]
[Produces("application/json")]
public class StudentCoursesController(
    IUserContext userContext,
    IStudentRepository repository) : ControllerBase
{
    /// <summary>Courses of the student's section. The student equivalent of the teacher's course list.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<StudentCourse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StudentCourse>>> Get(CancellationToken ct) =>
        Ok(await repository.GetCoursesAsync(userContext.StudentId, ct));
}
