using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerlessAPI.Authentication;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Repositories;

namespace ServerlessAPI.Controllers;

[ApiController]
[Authorize(Roles = nameof(Role.Teacher))]
[Route("docente/tareas")]
[Produces("application/json")]
public class TeacherAssignmentsController(
    ILogger<TeacherAssignmentsController> logger,
    IUserContext userContext,
    IAssignmentRepository repository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TeacherAssignment>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TeacherAssignment>>> List(
        [FromQuery] int? cursoId,
        CancellationToken ct) =>
        Ok(await repository.ListForTeacherAsync(userContext.TeacherId, cursoId, ct));

    [HttpPost]
    [ProducesResponseType<TeacherAssignment>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TeacherAssignment>> Create(
        [FromBody] CreateAssignmentRequest request,
        CancellationToken ct)
    {
        var assignment = await repository.CreateAsync(userContext.TeacherId, request, ct);
        logger.LogInformation("Assignment created. Id {AssignmentId}, course {CourseId}",
            assignment.Id, assignment.CourseId);
        return CreatedAtAction(nameof(List), new { id = assignment.Id }, assignment);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await repository.DeleteAsync(userContext.TeacherId, id, ct);
        return deleted ? NoContent() : NotFound();
    }
}
