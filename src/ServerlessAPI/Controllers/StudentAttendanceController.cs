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
[Route("alumno/asistencia")]
[Produces("application/json")]
public class StudentAttendanceController(
    IUserContext userContext,
    IStudentRepository repository) : ControllerBase
{
    /// <summary>The student's attendance history and total absences.</summary>
    [HttpGet]
    [ProducesResponseType<StudentAttendanceResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StudentAttendanceResponse>> Get(CancellationToken ct) =>
        Ok(await repository.GetAttendanceAsync(userContext.StudentId, ct));
}
