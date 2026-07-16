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
[Route("docente/perfil")]
[Produces("application/json")]
public class TeacherProfileController(
    ILogger<TeacherProfileController> logger,
    IUserContext userContext,
    IProfileRepository repository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileResponse>> Get(CancellationToken ct)
    {
        var teacherId = userContext.TeacherId;
        var profile = await repository.GetProfileAsync(teacherId, ct);

        return profile is null
            ? NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not found",
                Detail = $"El docente {teacherId} no existe.",
            })
            : Ok(profile);
    }

    [HttpPut]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileResponse>> Put(
        [FromBody] UpdateProfileRequest request,
        CancellationToken ct)
    {
        var teacherId = userContext.TeacherId;
        var profile = await repository.UpdateProfileAsync(teacherId, request, ct);

        logger.LogInformation("Profile updated for teacher {TeacherId}", teacherId);

        return Ok(profile);
    }
}
