using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerlessAPI.Authentication;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Repositories;

namespace ServerlessAPI.Controllers;

[ApiController]
[ApiExplorerSettings(GroupName = "Student")]
[Authorize(Roles = nameof(Role.Student))]
[Route("alumno/cursos")]
[Produces("application/json")]
public class StudentCoursesController(
    IUserContext userContext,
    IStudentRepository repository) : ControllerBase
{
    /// <summary>Cursos de la sección del alumno.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<StudentCourse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StudentCourse>>> Get(CancellationToken ct) =>
        Ok(await repository.GetCoursesAsync(userContext.StudentId, ct));
}
