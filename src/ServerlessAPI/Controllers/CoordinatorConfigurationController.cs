using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerlessAPI.Authentication;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Repositories;

namespace ServerlessAPI.Controllers;

[ApiController]
[Authorize(Roles = nameof(Role.Coordinator))]
[Route("coordinador/configuracion")]
[Produces("application/json")]
public class CoordinatorConfigurationController(
    ILogger<CoordinatorConfigurationController> logger,
    IUserContext userContext,
    IInstitutionalConfigurationRepository repository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<InstitutionalConfigurationResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<InstitutionalConfigurationResponse>> Get(
        CancellationToken ct)
    {
        var configuration = await repository.GetAsync(ct);

        return configuration is null
            ? NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not found",
                Detail = "La configuración institucional todavía no ha sido registrada.",
            })
            : Ok(configuration);
    }

    [HttpPut]
    [ProducesResponseType<InstitutionalConfigurationResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InstitutionalConfigurationResponse>> Put(
        [FromBody] UpdateInstitutionalConfigurationRequest request,
        CancellationToken ct)
    {
        var userId = userContext.UserId;

        var configuration = await repository.UpsertAsync(
            userId,
            request,
            ct);

        logger.LogInformation(
            "Institutional configuration updated by user {UserId}",
            userId);

        return Ok(configuration);
    }
}
