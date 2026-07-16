using System.Globalization;
using System.Text;
using AWS.Lambda.Powertools.Tracing;
using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Data;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;

namespace ServerlessAPI.Repositories;

public sealed class ReportRepository(SanLorenzoDbContext db) : IReportRepository
{
    [Tracing(SegmentName = "Generate report")]
    public async Task<ReportListItem> GenerateAsync(GenerateReportRequest request, CancellationToken ct = default)
    {
        var report = new Report
        {
            GradeLevel = request.GradeLevel,
            Term = request.Term,
            TeacherId = request.TeacherId,
            GeneratedAt = DateTime.UtcNow,
        };

        db.Reports.Add(report);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        var teacherName = report.TeacherId is null
            ? null
            : await db.Teachers.AsNoTracking()
                .Where(t => t.Id == report.TeacherId)
                .Select(t => t.FullName)
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

        return new ReportListItem(report.Id, report.GradeLevel, report.Term, teacherName, report.GeneratedAt);
    }

    [Tracing(SegmentName = "List reports")]
    public async Task<IReadOnlyList<ReportListItem>> ListAsync(CancellationToken ct = default) =>
        await db.Reports
            .AsNoTracking()
            .OrderByDescending(r => r.GeneratedAt)
            .Select(r => new ReportListItem(
                r.Id, r.GradeLevel, r.Term, r.Teacher == null ? null : r.Teacher.FullName, r.GeneratedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

    [Tracing(SegmentName = "Build report text")]
    public async Task<string?> BuildTextAsync(int reportId, CancellationToken ct = default)
    {
        var report = await db.Reports.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reportId, ct)
            .ConfigureAwait(false);

        if (report is null)
            return null;

        var schoolName = await db.SystemSettings.AsNoTracking()
            .Select(s => s.SchoolName)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false) ?? "Colegio San Lorenzo";

        var courses = await db.Courses.AsNoTracking()
            .Where(c => c.GradeLevel == report.GradeLevel
                        && (report.TeacherId == null || c.TeacherId == report.TeacherId))
            .OrderBy(c => c.Section).ThenBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, c.GradeLevel, c.Section, Teacher = c.Teacher.FullName })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var sb = new StringBuilder();
        sb.AppendLine("REPORTE DE PROMEDIOS");
        sb.AppendLine($"Colegio: {schoolName}");
        sb.AppendLine($"Grado: {report.GradeLevel}    Periodo: {report.Term}");
        sb.AppendLine($"Docente: {(report.TeacherId is null ? "Todos" : courses.FirstOrDefault()?.Teacher ?? "-")}");
        sb.AppendLine($"Generado: {report.GeneratedAt:yyyy-MM-dd HH:mm} UTC");
        sb.AppendLine(new string('=', 50));

        foreach (var course in courses)
        {
            var rows = await (
                    from s in db.Students.AsNoTracking()
                    where s.GradeLevel == course.GradeLevel && s.Section == course.Section
                    join g in db.Grades.AsNoTracking()
                        .Where(g => g.CourseId == course.Id && g.Term == report.Term)
                        on s.Id equals g.StudentId into gj
                    from g in gj.DefaultIfEmpty()
                    orderby s.Name
                    select new { s.Name, Average = (decimal?)(g == null ? null : g.Average) })
                .ToListAsync(ct)
                .ConfigureAwait(false);

            sb.AppendLine();
            sb.AppendLine($"Curso: {course.Name} ({course.GradeLevel}-{course.Section}) - {course.Teacher}");

            foreach (var row in rows)
            {
                var avg = row.Average is { } a ? a.ToString("F2", CultureInfo.InvariantCulture) : "-";
                sb.AppendLine($"  {row.Name.PadRight(30)} {avg}");
            }

            var graded = rows.Where(r => r.Average is not null).Select(r => r.Average!.Value).ToList();
            var courseAvg = graded.Count == 0
                ? "-"
                : Math.Round(graded.Average(), 2).ToString("F2", CultureInfo.InvariantCulture);
            sb.AppendLine($"  Promedio del curso: {courseAvg}");
        }

        if (courses.Count == 0)
        {
            sb.AppendLine();
            sb.AppendLine("(No hay cursos que coincidan con los filtros.)");
        }

        return sb.ToString();
    }
}
