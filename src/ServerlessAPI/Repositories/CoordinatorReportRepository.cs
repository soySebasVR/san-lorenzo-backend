using System.Text.Json;
using System.Text.Json.Serialization;
using AWS.Lambda.Powertools.Tracing;
using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Data;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Infrastructure;

namespace ServerlessAPI.Repositories;

public sealed class CoordinatorReportRepository(
    SanLorenzoDbContext db) : ICoordinatorReportRepository
{
    private const decimal PassingGrade = 11m;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    [Tracing(SegmentName = "List coordinator reports")]
    public async Task<IReadOnlyList<CoordinatorReportSummaryResponse>> GetAsync(
        int userId,
        string? reportType,
        CancellationToken ct = default)
    {
        var query = db.CoordinatorReports
            .AsNoTracking()
            .Where(r => r.GeneratedByUserId == userId);

        if (!string.IsNullOrWhiteSpace(reportType))
        {
            var parsedType = ParseReportType(reportType);

            query = query.Where(r => r.ReportType == parsedType);
        }

        return await query
            .OrderByDescending(r => r.GeneratedAt)
            .Select(r => new CoordinatorReportSummaryResponse(
                r.Id,
                r.ReportType.ToString(),
                r.GeneratedByUserId,
                r.GeneratedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    [Tracing(SegmentName = "Get coordinator report")]
    public async Task<CoordinatorReportResponse?> GetByIdAsync(
        int userId,
        int reportId,
        CancellationToken ct = default)
    {
        var report = await db.CoordinatorReports
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.Id == reportId
                     && r.GeneratedByUserId == userId,
                ct)
            .ConfigureAwait(false);

        return report is null
            ? null
            : MapResponse(report);
    }

    [Tracing(SegmentName = "Generate coordinator report")]
    public async Task<CoordinatorReportResponse> GenerateAsync(
        int userId,
        GenerateCoordinatorReportRequest request,
        CancellationToken ct = default)
    {
        await EnsureActiveCoordinatorAsync(userId, ct);

        var reportType = ParseReportType(request.ReportType);

        ValidateFilters(reportType, request);

        var resultJson = reportType switch
        {
            CoordinatorReportType.Attendance =>
                await GenerateAttendanceReportAsync(request, ct),

            CoordinatorReportType.Grades =>
                await GenerateGradesReportAsync(request, ct),

            CoordinatorReportType.Users =>
                await GenerateUsersReportAsync(request, ct),

            _ => throw new BadRequestException(
                "Unsupported report type."),
        };

        var report = new CoordinatorReport
        {
            ReportType = reportType,
            FiltersJson = JsonSerializer.Serialize(
                request,
                JsonOptions),
            ResultJson = resultJson,
            GeneratedByUserId = userId,
            GeneratedAt = DateTime.UtcNow,
        };

        db.CoordinatorReports.Add(report);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return MapResponse(report);
    }

    private async Task<string> GenerateAttendanceReportAsync(
        GenerateCoordinatorReportRequest request,
        CancellationToken ct)
    {
        var query = db.Attendance
            .AsNoTracking()
            .AsQueryable();

        if (request.StartDate is not null)
        {
            query = query.Where(
                a => a.Date >= request.StartDate.Value);
        }

        if (request.EndDate is not null)
        {
            query = query.Where(
                a => a.Date <= request.EndDate.Value);
        }

        if (request.CourseId is not null)
        {
            query = query.Where(
                a => a.CourseId == request.CourseId.Value);
        }

        var gradeLevel = NormalizeOptional(request.GradeLevel);

        if (gradeLevel is not null)
        {
            query = query.Where(
                a => a.Course.GradeLevel == gradeLevel);
        }

        var section = NormalizeOptional(request.Section);

        if (section is not null)
        {
            query = query.Where(
                a => a.Course.Section == section);
        }

        var grouped = await query
            .GroupBy(a => new
            {
                a.CourseId,
                CourseName = a.Course.Name,
                a.Course.GradeLevel,
                a.Course.Section,
                a.Date,
            })
            .Select(g => new
            {
                g.Key.CourseId,
                g.Key.CourseName,
                g.Key.GradeLevel,
                g.Key.Section,
                g.Key.Date,
                PresentCount = g.Count(a => a.Present),
                AbsentCount = g.Count(a => !a.Present),
                TotalCount = g.Count(),
            })
            .OrderByDescending(r => r.Date)
            .ThenBy(r => r.CourseName)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var rows = grouped
            .Select(r => new AttendanceCoordinatorReportRow(
                r.CourseId,
                r.CourseName,
                r.GradeLevel,
                r.Section,
                r.Date,
                r.PresentCount,
                r.AbsentCount,
                r.TotalCount))
            .ToList();

        return JsonSerializer.Serialize(rows, JsonOptions);
    }

    private async Task<string> GenerateGradesReportAsync(
        GenerateCoordinatorReportRequest request,
        CancellationToken ct)
    {
        var query = db.Grades
            .AsNoTracking()
            .AsQueryable();

        if (request.CourseId is not null)
        {
            query = query.Where(
                g => g.CourseId == request.CourseId.Value);
        }

        var term = NormalizeOptional(request.Term);

        if (term is not null)
        {
            query = query.Where(
                g => g.Term == term);
        }

        var gradeLevel = NormalizeOptional(request.GradeLevel);

        if (gradeLevel is not null)
        {
            query = query.Where(
                g => g.Course.GradeLevel == gradeLevel);
        }

        var section = NormalizeOptional(request.Section);

        if (section is not null)
        {
            query = query.Where(
                g => g.Course.Section == section);
        }

        var grouped = await query
            .GroupBy(g => new
            {
                g.CourseId,
                CourseName = g.Course.Name,
                g.Course.GradeLevel,
                g.Course.Section,
                g.Term,
            })
            .Select(g => new
            {
                g.Key.CourseId,
                g.Key.CourseName,
                g.Key.GradeLevel,
                g.Key.Section,
                g.Key.Term,
                StudentCount = g.Count(),
                AverageGrade = g.Average(x => x.Average),
                PassedCount =
                    g.Count(x => x.Average >= PassingGrade),
                FailedCount =
                    g.Count(x => x.Average < PassingGrade),
            })
            .OrderBy(r => r.CourseName)
            .ThenBy(r => r.Term)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var rows = grouped
            .Select(r => new GradesCoordinatorReportRow(
                r.CourseId,
                r.CourseName,
                r.GradeLevel,
                r.Section,
                r.Term,
                r.StudentCount,
                Math.Round(
                    r.AverageGrade,
                    2,
                    MidpointRounding.AwayFromZero),
                r.PassedCount,
                r.FailedCount))
            .ToList();

        return JsonSerializer.Serialize(rows, JsonOptions);
    }

    private async Task<string> GenerateUsersReportAsync(
        GenerateCoordinatorReportRequest request,
        CancellationToken ct)
    {
        var query = db.Users
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.UserRole))
        {
            if (!Enum.TryParse<Role>(
                    request.UserRole,
                    ignoreCase: true,
                    out var role))
            {
                throw new BadRequestException(
                    $"Invalid user role '{request.UserRole}'.");
            }

            query = query.Where(u => u.Role == role);
        }

        if (request.IsActive is not null)
        {
            query = query.Where(
                u => u.IsActive == request.IsActive.Value);
        }

        var grouped = await query
            .GroupBy(u => u.Role)
            .Select(g => new
            {
                Role = g.Key,
                ActiveCount = g.Count(u => u.IsActive),
                InactiveCount = g.Count(u => !u.IsActive),
                TotalCount = g.Count(),
            })
            .OrderBy(r => r.Role)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var rows = grouped
            .Select(r => new UsersCoordinatorReportRow(
                r.Role.ToString(),
                r.ActiveCount,
                r.InactiveCount,
                r.TotalCount))
            .ToList();

        return JsonSerializer.Serialize(rows, JsonOptions);
    }

    private async Task EnsureActiveCoordinatorAsync(
        int userId,
        CancellationToken ct)
    {
        var exists = await db.Users
            .AsNoTracking()
            .AnyAsync(
                u => u.Id == userId
                     && u.Role == Role.Coordinator
                     && u.IsActive,
                ct)
            .ConfigureAwait(false);

        if (!exists)
        {
            throw new NotFoundException(
                $"Coordinator user {userId} does not exist or is inactive.");
        }
    }

    private static CoordinatorReportType ParseReportType(
        string value)
    {
        if (!Enum.TryParse<CoordinatorReportType>(
                value,
                ignoreCase: true,
                out var reportType))
        {
            throw new BadRequestException(
                $"Invalid report type '{value}'.");
        }

        return reportType;
    }

    private static void ValidateFilters(
        CoordinatorReportType reportType,
        GenerateCoordinatorReportRequest request)
    {
        if (request.StartDate is not null &&
            request.EndDate is not null &&
            request.StartDate > request.EndDate)
        {
            throw new BadRequestException(
                "StartDate cannot be later than EndDate.");
        }

        switch (reportType)
        {
            case CoordinatorReportType.Attendance:
                if (!string.IsNullOrWhiteSpace(request.Term) ||
                    !string.IsNullOrWhiteSpace(request.UserRole) ||
                    request.IsActive is not null)
                {
                    throw new BadRequestException(
                        "Attendance reports do not accept Term, UserRole or IsActive filters.");
                }

                break;

            case CoordinatorReportType.Grades:
                if (request.StartDate is not null ||
                    request.EndDate is not null ||
                    !string.IsNullOrWhiteSpace(request.UserRole) ||
                    request.IsActive is not null)
                {
                    throw new BadRequestException(
                        "Grades reports do not accept date, UserRole or IsActive filters.");
                }

                break;

            case CoordinatorReportType.Users:
                if (request.StartDate is not null ||
                    request.EndDate is not null ||
                    request.CourseId is not null ||
                    !string.IsNullOrWhiteSpace(request.Term) ||
                    !string.IsNullOrWhiteSpace(request.GradeLevel) ||
                    !string.IsNullOrWhiteSpace(request.Section))
                {
                    throw new BadRequestException(
                        "Users reports only accept UserRole and IsActive filters.");
                }

                break;

            default:
                throw new BadRequestException(
                    "Unsupported report type.");
        }
    }

    private static CoordinatorReportResponse MapResponse(
        CoordinatorReport report)
    {
        return new CoordinatorReportResponse(
            report.Id,
            report.ReportType.ToString(),
            DeserializeJson(report.FiltersJson),
            DeserializeJson(report.ResultJson),
            report.GeneratedByUserId,
            report.GeneratedAt);
    }

    private static JsonElement DeserializeJson(string json)
    {
        return JsonSerializer.Deserialize<JsonElement>(
            json,
            JsonOptions);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
