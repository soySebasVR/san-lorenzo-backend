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
[Route("alumno/notas")]
[Produces("application/json")]
public class StudentGradesController(
    IUserContext userContext,
    IStudentRepository repository) : ControllerBase
{
    /// <summary>Cursos y notas del alumno.</summary>
    [HttpGet]
    [ProducesResponseType<StudentGradesResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StudentGradesResponse>> Get(
        [FromQuery] string? periodo,
        CancellationToken ct) =>
        Ok(await repository.GetGradesAsync(userContext.StudentId, periodo, ct));
}
