using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerlessAPI.Authentication;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Repositories;

namespace ServerlessAPI.Controllers;

[ApiController]
[Authorize(Roles = nameof(Role.Coordinator))]
[Route("coordinador/perfil")]
[Produces("application/json")]
public class CoordinatorProfileController(
    ILogger<CoordinatorProfileController> logger,
    IUserContext userContext,
    ICoordinatorProfileRepository repository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CoordinatorProfileResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CoordinatorProfileResponse>> Get(
        CancellationToken ct)
    {
        var userId = userContext.UserId;

        var profile = await repository.GetAsync(userId, ct);

        return profile is null
            ? NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not found",
                Detail = $"El coordinador asociado al usuario {userId} no existe o está inactivo.",
            })
            : Ok(profile);
    }

    [HttpPut]
    [ProducesResponseType<CoordinatorProfileResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CoordinatorProfileResponse>> Put(
        [FromBody] UpdateCoordinatorProfileRequest request,
        CancellationToken ct)
    {
        var userId = userContext.UserId;

        var profile = await repository.UpdateAsync(
            userId,
            request,
            ct);

        logger.LogInformation(
            "Coordinator profile updated for user {UserId}",
            userId);

        return Ok(profile);
    }
}
