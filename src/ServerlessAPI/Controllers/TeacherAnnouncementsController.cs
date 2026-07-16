using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerlessAPI.Authentication;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Repositories;

namespace ServerlessAPI.Controllers;

[ApiController]
[Authorize(Roles = nameof(Role.Teacher))]
[Route("docente/comunicaciones")]
[Produces("application/json")]
public class TeacherAnnouncementsController(
    ILogger<TeacherAnnouncementsController> logger,
    IUserContext userContext,
    IAnnouncementRepository repository) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<AnnouncementResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AnnouncementResponse>> Post(
        [FromBody] SendAnnouncementRequest request,
        CancellationToken ct)
    {
        var teacherId = userContext.TeacherId;
        var announcement = await repository.SendAnnouncementAsync(teacherId, request, ct);

        logger.LogInformation(
            "Announcement sent. Id {AnnouncementId}, teacher {TeacherId}, {Recipients} recipients",
            announcement.Id, teacherId, announcement.RecipientCount);

        return CreatedAtAction(nameof(Post), new { id = announcement.Id }, announcement);
    }
}
