using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerlessAPI.Authentication;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Repositories;

namespace ServerlessAPI.Controllers;

[ApiController]
[ApiExplorerSettings(GroupName = "Student")]
[Authorize(Roles = nameof(Role.Student))]
[Route("alumno/tareas")]
[Produces("application/json")]
public class StudentAssignmentsController(
    IUserContext userContext,
    IAssignmentRepository repository) : ControllerBase
{
    /// <summary>Tasks and exams of the student's section. Filter by `tipo` (Task/Exam).</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<StudentAssignment>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StudentAssignment>>> List(
        [FromQuery] string? tipo,
        CancellationToken ct) =>
        Ok(await repository.ListForStudentAsync(userContext.StudentId, tipo, ct));
}
