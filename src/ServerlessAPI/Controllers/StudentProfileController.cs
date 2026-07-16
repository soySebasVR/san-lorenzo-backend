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
[Route("alumno/perfil")]
[Produces("application/json")]
public class StudentProfileController(
    ILogger<StudentProfileController> logger,
    IUserContext userContext,
    IStudentRepository repository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<StudentProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentProfileResponse>> Get(CancellationToken ct)
    {
        var profile = await repository.GetProfileAsync(userContext.StudentId, ct);

        return profile is null
            ? NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not found",
                Detail = $"El alumno {userContext.StudentId} no existe.",
            })
            : Ok(profile);
    }

    [HttpPut]
    [ProducesResponseType<StudentProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentProfileResponse>> Put(
        [FromBody] UpdateStudentProfileRequest request,
        CancellationToken ct)
    {
        var studentId = userContext.StudentId;
        var profile = await repository.UpdateProfileAsync(studentId, request, ct);

        logger.LogInformation("Profile updated for student {StudentId}", studentId);

        return Ok(profile);
    }
}
