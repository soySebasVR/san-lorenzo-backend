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
[Route("docente/horarios")]
[Produces("application/json")]
public class TeacherScheduleController(
    ILogger<TeacherScheduleController> logger,
    IUserContext userContext,
    IScheduleRepository repository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ScheduleResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ScheduleResponse>> Get(CancellationToken ct)
    {
        var teacherId = userContext.TeacherId;
        var schedule = await repository.GetScheduleAsync(teacherId, ct);

        logger.LogInformation(
            "Listed {Total} classes for teacher {TeacherId}", schedule.Classes.Count, teacherId);

        return Ok(schedule);
    }
}
