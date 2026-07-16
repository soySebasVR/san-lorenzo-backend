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
[Route("alumno/horarios")]
[Produces("application/json")]
public class StudentScheduleController(
    IUserContext userContext,
    IStudentRepository repository) : ControllerBase
{
    /// <summary>Horario semanal del alumno.</summary>
    [HttpGet]
    [ProducesResponseType<StudentScheduleResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StudentScheduleResponse>> Get(CancellationToken ct) =>
        Ok(await repository.GetScheduleAsync(userContext.StudentId, ct));
}
