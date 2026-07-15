using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerlessAPI.Authentication;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Repositories;

namespace ServerlessAPI.Controllers;

[ApiController]
[Authorize(Roles = nameof(Role.Teacher))]
[Route("docente/inicio")]
[Produces("application/json")]
public class DashboardController(
    ILogger<DashboardController> logger,
    IUserContext userContext,
    IDashboardRepository repository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<DashboardResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardResponse>> Get(CancellationToken ct)
    {
        var teacherId = userContext.TeacherId;
        var dashboard = await repository.GetDashboardAsync(teacherId, ct);

        logger.LogInformation(
            "Dashboard loaded for teacher {TeacherId}: {Courses} courses, {Pending} pending",
            teacherId, dashboard.TotalCourses, dashboard.Pending);

        return Ok(dashboard);
    }
}
