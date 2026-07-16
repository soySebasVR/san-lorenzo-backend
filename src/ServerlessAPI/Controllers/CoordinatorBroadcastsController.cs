using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Repositories;

namespace ServerlessAPI.Controllers;

[ApiController]
[Authorize(Roles = nameof(Role.Coordinator))]
[Route("coordinador/comunicados")]
[Produces("application/json")]
public class CoordinatorBroadcastsController(
    ILogger<CoordinatorBroadcastsController> logger,
    IBroadcastRepository repository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BroadcastResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BroadcastResponse>>> List(CancellationToken ct) =>
        Ok(await repository.ListAsync(ct));

    /// <summary>Schedules a broadcast. There is no delivery mechanism yet; it is stored and listed.</summary>
    [HttpPost]
    [ProducesResponseType<BroadcastResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BroadcastResponse>> Create(
        [FromBody] CreateBroadcastRequest request,
        CancellationToken ct)
    {
        var broadcast = await repository.CreateAsync(request, ct);
        logger.LogInformation("Broadcast scheduled. Id {BroadcastId}, audience {Audience}",
            broadcast.Id, broadcast.Audience);
        return CreatedAtAction(nameof(List), new { id = broadcast.Id }, broadcast);
    }
}
