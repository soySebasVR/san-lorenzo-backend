using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerlessAPI.Authentication;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Repositories;

namespace ServerlessAPI.Controllers;

[ApiController]
[Authorize(Roles = nameof(Role.Coordinator))]
[Route("coordinador/comunicados")]
[Produces("application/json")]
public sealed class CoordinatorAnnouncementsController(
    ILogger<CoordinatorAnnouncementsController> logger,
    IUserContext userContext,
    ICoordinatorAnnouncementRepository repository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CoordinatorAnnouncementResponse>>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<
        IReadOnlyList<CoordinatorAnnouncementResponse>>> Get(
        [FromQuery] string? status,
        CancellationToken ct)
    {
        var announcements = await repository.GetAsync(
            userContext.UserId,
            status,
            ct);

        return Ok(announcements);
    }

    [HttpPost]
    [ProducesResponseType<CoordinatorAnnouncementResponse>(
        StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CoordinatorAnnouncementResponse>> Post(
        [FromBody] CreateCoordinatorAnnouncementRequest request,
        CancellationToken ct)
    {
        var userId = userContext.UserId;

        var announcement = await repository.CreateAsync(
            userId,
            request,
            ct);

        logger.LogInformation(
            "Coordinator announcement {AnnouncementId} created by user {UserId}",
            announcement.Id,
            userId);

        return StatusCode(
            StatusCodes.Status201Created,
            announcement);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<CoordinatorAnnouncementResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CoordinatorAnnouncementResponse>> Put(
        [FromRoute] int id,
        [FromBody] UpdateCoordinatorAnnouncementRequest request,
        CancellationToken ct)
    {
        var userId = userContext.UserId;

        var announcement = await repository.UpdateAsync(
            userId,
            id,
            request,
            ct);

        logger.LogInformation(
            "Coordinator announcement {AnnouncementId} updated by user {UserId}",
            id,
            userId);

        return Ok(announcement);
    }
}
