using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Repositories;

namespace ServerlessAPI.Controllers;

[ApiController]
[ApiExplorerSettings(GroupName = "Coordinator")]
[Authorize(Roles = nameof(Role.Coordinator))]
[Route("coordinador/reportes")]
[Produces("application/json")]
public class CoordinatorReportsController(
    ILogger<CoordinatorReportsController> logger,
    IReportRepository repository) : ControllerBase
{
    /// <summary>Historial de reportes generados..</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ReportListItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReportListItem>>> List(CancellationToken ct) =>
        Ok(await repository.ListAsync(ct));

    [HttpPost]
    [ProducesResponseType<ReportListItem>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReportListItem>> Generate(
        [FromBody] GenerateReportRequest request,
        CancellationToken ct)
    {
        var report = await repository.GenerateAsync(request, ct);
        logger.LogInformation("Reporte generado. Id {ReportId}, grade {Grade}, term {Term}",
            report.Id, report.GradeLevel, report.Term);
        return CreatedAtAction(nameof(List), new { id = report.Id }, report);
    }

    /// <summary>Downloads the averages report as a plain-text file.</summary>
    [HttpGet("{id:int}/descargar")]
    [Produces("text/plain")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(int id, CancellationToken ct)
    {
        var text = await repository.BuildTextAsync(id, ct);

        if (text is null)
            return NotFound();

        var bytes = Encoding.UTF8.GetBytes(text);
        return File(bytes, "text/plain; charset=utf-8", $"reporte-{id}.txt");
    }
}
