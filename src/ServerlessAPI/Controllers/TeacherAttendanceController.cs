using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ServerlessAPI.Authentication;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Repositories;

namespace ServerlessAPI.Controllers;

[ApiController]
[Authorize(Roles = nameof(Role.Teacher))]
[Route("docente/asistencia")]
[Produces("application/json")]
public class TeacherAttendanceController(
    ILogger<TeacherAttendanceController> logger,
    IUserContext userContext,
    IAttendanceRepository repository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<AttendanceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttendanceResponse>> Get(
        [FromQuery, BindRequired] int courseId,
        [FromQuery] DateOnly? date,
        CancellationToken ct)
    {
        var teacherId = userContext.TeacherId;
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var attendance = await repository.GetAttendanceAsync(teacherId, courseId, targetDate, ct);

        logger.LogInformation(
            "Listed attendance. Course {CourseId}, date {Date}, {Total} students",
            courseId, targetDate, attendance.Students.Count);

        return Ok(attendance);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Post(
        [FromBody] SaveAttendanceRequest request,
        CancellationToken ct)
    {
        var teacherId = userContext.TeacherId;
        await repository.SaveAttendanceAsync(teacherId, request, ct);

        logger.LogInformation(
            "Attendance saved. Course {CourseId}, date {Date}, {Total} entries, teacher {TeacherId}",
            request.CourseId, request.Date, request.Entries.Count, teacherId);

        return NoContent();
    }
}
