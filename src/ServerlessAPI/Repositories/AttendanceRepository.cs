using AWS.Lambda.Powertools.Tracing;
using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Data;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Infrastructure;

namespace ServerlessAPI.Repositories;

public sealed class AttendanceRepository(SanLorenzoDbContext db) : IAttendanceRepository
{
    [Tracing(SegmentName = "List attendance")]
    public async Task<AttendanceResponse> GetAttendanceAsync(
        int teacherId, int courseId, DateOnly date, CancellationToken ct = default)
    {
        var course = await db.Courses
            .AsNoTracking()
            .Where(c => c.Id == courseId && c.TeacherId == teacherId)
            .Select(c => new { c.Name, c.GradeLevel, c.Section })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (course is null)
            throw new NotFoundException($"Course {courseId} does not exist or is not yours.");

        // No record for that date means present.
        var students = await (
                from s in db.Students.AsNoTracking()
                where s.GradeLevel == course.GradeLevel && s.Section == course.Section
                join a in db.Attendance.AsNoTracking().Where(x => x.Date == date && x.CourseId == courseId)
                    on s.Id equals a.StudentId
                    into records
                from a in records.DefaultIfEmpty()
                orderby s.Name
                select new StudentAttendance(s.Id, s.Name, a == null || a.Present))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new AttendanceResponse(courseId, course.Name, date, students);
    }

    [Tracing(SegmentName = "Save attendance")]
    public async Task SaveAttendanceAsync(
        int teacherId, SaveAttendanceRequest request, CancellationToken ct = default)
    {
        var course = await db.Courses
            .AsNoTracking()
            .Where(c => c.Id == request.CourseId && c.TeacherId == teacherId)
            .Select(c => new { c.GradeLevel, c.Section })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (course is null)
            throw new ForbiddenException("Course does not belong to this teacher.");

        var studentIds = request.Entries.Select(e => e.StudentId).Distinct().ToList();

        // Every student sent must be in this course's section.
        var validStudents = await db.Students
            .AsNoTracking()
            .CountAsync(
                s => s.GradeLevel == course.GradeLevel
                     && s.Section == course.Section
                     && studentIds.Contains(s.Id),
                ct)
            .ConfigureAwait(false);

        if (validStudents != studentIds.Count)
            throw new ForbiddenException("Some student does not belong to this course.");

        // One query for what already exists; avoids an N+1 lookup per student.
        var existing = await db.Attendance
            .Where(a => a.CourseId == request.CourseId
                        && a.Date == request.Date
                        && studentIds.Contains(a.StudentId))
            .ToDictionaryAsync(a => a.StudentId, ct)
            .ConfigureAwait(false);

        foreach (var entry in request.Entries)
        {
            if (existing.TryGetValue(entry.StudentId, out var attendance))
            {
                attendance.Present = entry.Present;
            }
            else
            {
                db.Attendance.Add(new Attendance
                {
                    StudentId = entry.StudentId,
                    CourseId = request.CourseId,
                    Date = request.Date,
                    Present = entry.Present,
                });
            }
        }

        // Single SaveChanges: EF batches the inserts and updates in one transaction.
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
