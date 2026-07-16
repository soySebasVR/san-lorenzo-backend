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
[Route("alumno/inicio")]
[Produces("application/json")]
public class StudentDashboardController(
    IUserContext userContext,
    IStudentRepository repository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<StudentDashboardResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StudentDashboardResponse>> Get(CancellationToken ct) =>
        Ok(await repository.GetDashboardAsync(userContext.StudentId, ct));
}
