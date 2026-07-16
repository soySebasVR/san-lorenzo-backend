using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Repositories;

namespace ServerlessAPI.Controllers;

[ApiController]
[ApiExplorerSettings(GroupName = "Coordinator")]
[Authorize(Roles = nameof(Role.Coordinator))]
[Route("coordinador/conducta")]
[Produces("application/json")]
public class CoordinatorBehaviorController(
    ILogger<CoordinatorBehaviorController> logger,
    IBehaviorRepository repository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BehaviorReportResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BehaviorReportResponse>>> List(CancellationToken ct) =>
        Ok(await repository.ListAsync(ct));

    [HttpPost]
    [ProducesResponseType<BehaviorReportResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BehaviorReportResponse>> Create(
        [FromBody] CreateBehaviorReportRequest request,
        CancellationToken ct)
    {
        var report = await repository.CreateAsync(request, ct);
        logger.LogInformation("Behavior report created. Id {Id}, student {StudentId}",
            report.Id, report.StudentId);
        return CreatedAtAction(nameof(List), new { id = report.Id }, report);
    }
}
