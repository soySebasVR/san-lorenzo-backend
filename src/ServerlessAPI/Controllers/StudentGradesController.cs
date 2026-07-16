using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerlessAPI.Authentication;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Repositories;

namespace ServerlessAPI.Controllers;

[ApiController]
[Authorize(Roles = nameof(Role.Student))]
[Route("alumno/notas")]
[Produces("application/json")]
public class StudentGradesController(
    IUserContext userContext,
    IStudentRepository repository) : ControllerBase
{
    /// <summary>The student's courses with their grade. Defaults to the most recent term.</summary>
    [HttpGet]
    [ProducesResponseType<StudentGradesResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StudentGradesResponse>> Get(
        [FromQuery] string? periodo,
        CancellationToken ct) =>
        Ok(await repository.GetGradesAsync(userContext.StudentId, periodo, ct));
}
