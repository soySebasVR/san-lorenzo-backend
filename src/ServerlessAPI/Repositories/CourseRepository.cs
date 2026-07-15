using AWS.Lambda.Powertools.Tracing;
using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Data;
using ServerlessAPI.Dtos;

namespace ServerlessAPI.Repositories;

public sealed class CourseRepository(SanLorenzoDbContext db) : ICourseRepository
{
    [Tracing(SegmentName = "List courses")]
    public async Task<IReadOnlyList<CourseResponse>> GetCoursesAsync(
        int teacherId, string? section, string? gradeLevel, string? name, CancellationToken ct = default)
    {
        var query = db.Courses
            .AsNoTracking()
            .Where(c => c.TeacherId == teacherId);

        if (!string.IsNullOrWhiteSpace(section))
            query = query.Where(c => c.Section == section);

        if (!string.IsNullOrWhiteSpace(gradeLevel))
            query = query.Where(c => c.GradeLevel == gradeLevel);

        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(c => c.Name == name);

        return await query
            .OrderBy(c => c.GradeLevel).ThenBy(c => c.Section).ThenBy(c => c.Name)
            .Select(c => new CourseResponse(c.Id, c.Name, c.GradeLevel, c.Section, c.Color))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    [Tracing(SegmentName = "Get course")]
    public async Task<CourseResponse?> GetCourseAsync(int courseId, int teacherId, CancellationToken ct = default)
    {
        // Ownership goes in the WHERE, not in a check afterwards: someone else's course
        // must be indistinguishable from one that does not exist.
        return await db.Courses
            .AsNoTracking()
            .Where(c => c.Id == courseId && c.TeacherId == teacherId)
            .Select(c => new CourseResponse(c.Id, c.Name, c.GradeLevel, c.Section, c.Color))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }
}
