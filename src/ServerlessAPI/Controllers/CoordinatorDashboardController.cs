using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Repositories;

namespace ServerlessAPI.Controllers;

[ApiController]
[Authorize(Roles = nameof(Role.Coordinator))]
[Route("coordinador/inicio")]
[Produces("application/json")]
public class CoordinatorDashboardController(ICoordinatorDashboardRepository repository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CoordinatorDashboardResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CoordinatorDashboardResponse>> Get(CancellationToken ct) =>
        Ok(await repository.GetDashboardAsync(ct));
}
