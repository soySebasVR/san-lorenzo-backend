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
    [ProducesResponseType<CoordinatorProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CoordinatorProfileResponse>> Get(CancellationToken ct)
    {
        var profile = await repository.GetAsync(userContext.UserId, ct);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut]
    [ProducesResponseType<CoordinatorProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CoordinatorProfileResponse>> Put(
        [FromBody] UpdateCoordinatorProfileRequest request,
        CancellationToken ct)
    {
        var profile = await repository.UpdateAsync(userContext.UserId, request, ct);
        logger.LogInformation("Coordinator profile updated for user {UserId}", userContext.UserId);
        return Ok(profile);
    }
}
