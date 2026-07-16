using AWS.Lambda.Powertools.Tracing;
using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Data;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;

namespace ServerlessAPI.Repositories;

public sealed class CoordinatorDashboardRepository(SanLorenzoDbContext db) : ICoordinatorDashboardRepository
{
    [Tracing(SegmentName = "Coordinator dashboard")]
    public async Task<CoordinatorDashboardResponse> GetDashboardAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var totalStudents = await db.Students.AsNoTracking().CountAsync(ct).ConfigureAwait(false);

        var activeTeachers = await db.Users.AsNoTracking()
            .CountAsync(u => u.Role == Role.Teacher && u.IsActive, ct).ConfigureAwait(false);

        var todayTotal = await db.Attendance.AsNoTracking()
            .CountAsync(a => a.Date == today, ct).ConfigureAwait(false);
        var todayPresent = await db.Attendance.AsNoTracking()
            .CountAsync(a => a.Date == today && a.Present, ct).ConfigureAwait(false);
        var attendanceTodayPct = todayTotal == 0 ? 0 : (int)Math.Round(todayPresent * 100.0 / todayTotal);

        // Grades compliance = courses that have at least one grade / total courses.
        var totalCourses = await db.Courses.AsNoTracking().CountAsync(ct).ConfigureAwait(false);
        var coursesWithGrades = await db.Grades.AsNoTracking()
            .Select(g => g.CourseId).Distinct().CountAsync(ct).ConfigureAwait(false);
        var compliancePct = totalCourses == 0 ? 0 : (int)Math.Round(coursesWithGrades * 100.0 / totalCourses);

        return new CoordinatorDashboardResponse(
            totalStudents, activeTeachers, attendanceTodayPct, compliancePct);
    }
}
