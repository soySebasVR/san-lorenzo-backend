using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Repositories;

namespace ServerlessAPI.Controllers;

[ApiController]
[ApiExplorerSettings(GroupName = "Coordinator")]
[Authorize(Roles = nameof(Role.Coordinator))]
[Route("coordinador/horarios")]
[Produces("application/json")]
public class CoordinatorScheduleController(
    ILogger<CoordinatorScheduleController> logger,
    IScheduleAdminRepository repository) : ControllerBase
{
    /// <summary>Every schedule slot across all sections.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AdminScheduleSlot>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminScheduleSlot>>> List(CancellationToken ct) =>
        Ok(await repository.ListAsync(ct));

    [HttpPost]
    [ProducesResponseType<AdminScheduleSlot>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminScheduleSlot>> Create(
        [FromBody] SaveScheduleSlotRequest request,
        CancellationToken ct)
    {
        var slot = await repository.CreateAsync(request, ct);
        logger.LogInformation("Schedule slot created. Id {SlotId}, course {CourseId}", slot.Id, slot.CourseId);
        return CreatedAtAction(nameof(List), new { id = slot.Id }, slot);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await repository.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
