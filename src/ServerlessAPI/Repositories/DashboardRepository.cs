using AWS.Lambda.Powertools.Tracing;
using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Data;
using ServerlessAPI.Dtos;

namespace ServerlessAPI.Repositories;

public sealed class DashboardRepository(SanLorenzoDbContext db) : IDashboardRepository
{
    [Tracing(SegmentName = "Teacher dashboard")]
    public async Task<DashboardResponse> GetDashboardAsync(int teacherId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var courses = await db.Courses
            .AsNoTracking()
            .Where(c => c.TeacherId == teacherId)
            .OrderBy(c => c.GradeLevel).ThenBy(c => c.Section)
            .Select(c => new CourseSummary(c.Id, c.Name, c.GradeLevel, c.Section, c.ScheduleText))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var totalStudents = await db.Students
            .AsNoTracking()
            .CountAsync(s => s.Course.TeacherId == teacherId, ct)
            .ConfigureAwait(false);

        // Pending = courses with no attendance recorded today.
        var pending = await db.Courses
            .AsNoTracking()
            .CountAsync(
                c => c.TeacherId == teacherId && !c.AttendanceRecords.Any(a => a.Date == today),
                ct)
            .ConfigureAwait(false);

        return new DashboardResponse(courses.Count, totalStudents, pending, courses);
    }
}
