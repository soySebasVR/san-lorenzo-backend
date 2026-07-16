using AWS.Lambda.Powertools.Tracing;
using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Data;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Infrastructure;

namespace ServerlessAPI.Repositories;

public sealed class CourseAdminRepository(SanLorenzoDbContext db) : ICourseAdminRepository
{
    [Tracing(SegmentName = "Admin list courses")]
    public async Task<IReadOnlyList<AdminCourse>> ListAsync(CancellationToken ct = default) =>
        await db.Courses
            .AsNoTracking()
            .OrderBy(c => c.GradeLevel).ThenBy(c => c.Section).ThenBy(c => c.Name)
            .Select(c => new AdminCourse(
                c.Id, c.Name, c.GradeLevel, c.Section, c.TeacherId, c.Teacher.FullName, c.Color))
            .ToListAsync(ct)
            .ConfigureAwait(false);

    [Tracing(SegmentName = "Admin get course")]
    public async Task<AdminCourse?> GetAsync(int courseId, CancellationToken ct = default) =>
        await db.Courses
            .AsNoTracking()
            .Where(c => c.Id == courseId)
            .Select(c => new AdminCourse(
                c.Id, c.Name, c.GradeLevel, c.Section, c.TeacherId, c.Teacher.FullName, c.Color))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

    [Tracing(SegmentName = "Create course")]
    public async Task<AdminCourse> CreateAsync(SaveCourseRequest request, CancellationToken ct = default)
    {
        await RequireTeacherAsync(request.TeacherId, ct);

        var course = new Course
        {
            Name = request.Name,
            GradeLevel = request.GradeLevel,
            Section = request.Section,
            TeacherId = request.TeacherId,
            Color = request.Color,
        };

        db.Courses.Add(course);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return await GetAsync(course.Id, ct).ConfigureAwait(false)
               ?? throw new NotFoundException($"Course {course.Id} not found after creation.");
    }

    [Tracing(SegmentName = "Update course")]
    public async Task<AdminCourse?> UpdateAsync(int courseId, SaveCourseRequest request, CancellationToken ct = default)
    {
        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == courseId, ct).ConfigureAwait(false);
        if (course is null)
            return null;

        await RequireTeacherAsync(request.TeacherId, ct);

        course.Name = request.Name;
        course.GradeLevel = request.GradeLevel;
        course.Section = request.Section;
        course.TeacherId = request.TeacherId;
        course.Color = request.Color;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return await GetAsync(course.Id, ct).ConfigureAwait(false);
    }

    [Tracing(SegmentName = "Delete course")]
    public async Task<bool> DeleteAsync(int courseId, CancellationToken ct = default)
    {
        var course = await db.Courses.FirstOrDefaultAsync(c => c.Id == courseId, ct).ConfigureAwait(false);
        if (course is null)
            return false;

        // Grades and schedule slots reference the course; refuse rather than cascade-wipe
        // academic history.
        var hasGrades = await db.Grades.AnyAsync(g => g.CourseId == courseId, ct).ConfigureAwait(false);
        if (hasGrades)
            throw new ForbiddenException("This course has grades and cannot be deleted.");

        db.Courses.Remove(course);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    [Tracing(SegmentName = "List teachers")]
    public async Task<IReadOnlyList<TeacherOption>> GetTeachersAsync(CancellationToken ct = default) =>
        await db.Teachers
            .AsNoTracking()
            .OrderBy(t => t.FullName)
            .Select(t => new TeacherOption(t.Id, t.FullName))
            .ToListAsync(ct)
            .ConfigureAwait(false);

    [Tracing(SegmentName = "List grade sections")]
    public async Task<IReadOnlyList<GradeSection>> GetGradeSectionsAsync(CancellationToken ct = default)
    {
        var rows = await db.Courses
            .AsNoTracking()
            .Select(c => new { c.GradeLevel, c.Section })
            .Distinct()
            .OrderBy(g => g.GradeLevel).ThenBy(g => g.Section)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows.Select(r => new GradeSection(r.GradeLevel, r.Section)).ToList();
    }

    private async Task RequireTeacherAsync(int teacherId, CancellationToken ct)
    {
        if (!await db.Teachers.AnyAsync(t => t.Id == teacherId, ct).ConfigureAwait(false))
            throw new ForbiddenException($"Teacher {teacherId} does not exist.");
    }
}
