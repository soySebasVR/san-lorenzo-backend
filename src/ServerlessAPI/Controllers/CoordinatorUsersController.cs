using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Repositories;

namespace ServerlessAPI.Controllers;

[ApiController]
[ApiExplorerSettings(GroupName = "Coordinator")]
[Authorize(Roles = nameof(Role.Coordinator))]
[Route("coordinador/usuarios")]
[Produces("application/json")]
public class CoordinatorUsersController(
    ILogger<CoordinatorUsersController> logger,
    IUserAdminRepository repository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UserListItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserListItem>>> List(
        [FromQuery] string? search,
        CancellationToken ct) =>
        Ok(await repository.ListAsync(search, ct));

    [HttpGet("{id:int}")]
    [ProducesResponseType<UserDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDetail>> Get(int id, CancellationToken ct)
    {
        var user = await repository.GetAsync(id, ct);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    [ProducesResponseType<UserDetail>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserDetail>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken ct)
    {
        var user = await repository.CreateAsync(request, ct);
        logger.LogInformation("User created. Id {UserId}, role {Role}", user.Id, user.Role);
        return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<UserDetail>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDetail>> Update(
        int id,
        [FromBody] UpdateUserRequest request,
        CancellationToken ct)
    {
        var user = await repository.UpdateAsync(id, request, ct);

        if (user is null)
            return NotFound();

        logger.LogInformation("User {UserId} updated (active={IsActive})", id, user.IsActive);
        return Ok(user);
    }
}
