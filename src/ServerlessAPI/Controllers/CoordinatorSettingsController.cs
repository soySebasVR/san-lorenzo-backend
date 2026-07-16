using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Repositories;

namespace ServerlessAPI.Controllers;

[ApiController]
[ApiExplorerSettings(GroupName = "Coordinator")]
[Authorize(Roles = nameof(Role.Coordinator))]
[Route("coordinador/configuracion")]
[Produces("application/json")]
public class CoordinatorSettingsController(
    ILogger<CoordinatorSettingsController> logger,
    ISettingsRepository repository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<SettingsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<SettingsResponse>> Get(CancellationToken ct) =>
        Ok(await repository.GetAsync(ct));

    [HttpPut]
    [ProducesResponseType<SettingsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SettingsResponse>> Update(
        [FromBody] UpdateSettingsRequest request,
        CancellationToken ct)
    {
        var settings = await repository.UpdateAsync(request, ct);
        logger.LogInformation("System settings updated (term {Term})", settings.CurrentTerm);
        return Ok(settings);
    }
}
