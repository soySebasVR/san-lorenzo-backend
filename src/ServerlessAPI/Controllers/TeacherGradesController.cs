using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ServerlessAPI.Authentication;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Repositories;

namespace ServerlessAPI.Controllers;

[ApiController]
[ApiExplorerSettings(GroupName = "Teacher")]
[Authorize(Roles = nameof(Role.Teacher))]
[Route("docente/notas")]
[Produces("application/json")]
public class TeacherGradesController(
    ILogger<TeacherGradesController> logger,
    IUserContext userContext,
    IGradeRepository repository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<GradesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GradesResponse>> Get(
        [FromQuery, BindRequired] string course,
        [FromQuery, BindRequired] string gradeLevel,
        [FromQuery, BindRequired] string section,
        [FromQuery, BindRequired] string term,
        CancellationToken ct)
    {
        var teacherId = userContext.TeacherId;
        var grades = await repository.GetGradesAsync(teacherId, course, gradeLevel, section, term, ct);

        logger.LogInformation(
            "Listed {Total} grades for teacher {TeacherId}, course {Course} {GradeLevel}-{Section}",
            grades.Entries.Count, teacherId, course, gradeLevel, section);

        return Ok(grades);
    }

    [HttpPut("{studentId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Put(
        int studentId,
        [FromBody] UpdateGradeRequest request,
        CancellationToken ct)
    {
        var teacherId = userContext.TeacherId;
        await repository.UpsertGradeAsync(studentId, teacherId, request, ct);

        logger.LogInformation(
            "Grade saved. Student {StudentId}, course {CourseId}, term {Term}, teacher {TeacherId}",
            studentId, request.CourseId, request.Term, teacherId);

        return NoContent();
    }
}
