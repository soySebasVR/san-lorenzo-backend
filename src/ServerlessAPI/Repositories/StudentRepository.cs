using AWS.Lambda.Powertools.Tracing;
using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Data;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Infrastructure;

namespace ServerlessAPI.Repositories;

public sealed class StudentRepository(SanLorenzoDbContext db) : IStudentRepository
{
    /// <summary>The section a student belongs to. Everything else hangs off this.</summary>
    private async Task<Student> RequireStudentAsync(int studentId, CancellationToken ct)
    {
        var student = await db.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == studentId, ct)
            .ConfigureAwait(false);

        return student ?? throw new NotFoundException($"Student {studentId} does not exist.");
    }

    /// <summary>Most recent term with grades for this student, or null if there are none.</summary>
    private async Task<string?> LatestTermAsync(int studentId, CancellationToken ct) =>
        await db.Grades
            .AsNoTracking()
            .Where(g => g.StudentId == studentId)
            .Select(g => g.Term)
            .OrderByDescending(t => t)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

    [Tracing(SegmentName = "Student dashboard")]
    public async Task<StudentDashboardResponse> GetDashboardAsync(int studentId, CancellationToken ct = default)
    {
        var student = await RequireStudentAsync(studentId, ct);
        var term = await LatestTermAsync(studentId, ct);

        var courses = await CoursesWithGradeAsync(student, term, studentId, ct);

        var graded = courses.Where(c => c.Average is not null).ToList();
        var overall = graded.Count == 0
            ? 0m
            : Math.Round(graded.Average(c => c.Average!.Value), 2, MidpointRounding.AwayFromZero);

        var total = await db.Attendance.AsNoTracking()
            .CountAsync(a => a.StudentId == studentId, ct).ConfigureAwait(false);
        var present = await db.Attendance.AsNoTracking()
            .CountAsync(a => a.StudentId == studentId && a.Present, ct).ConfigureAwait(false);

        // No records yet reads as full attendance rather than 0%.
        var attendancePct = total == 0 ? 100 : (int)Math.Round(present * 100.0 / total);

        return new StudentDashboardResponse(
            overall,
            attendancePct,
            courses.Count,
            courses.Select(c => new StudentCourseGrade(c.CourseId, c.Name, c.TeacherName, c.Average ?? 0m)).ToList());
    }

    [Tracing(SegmentName = "Student courses")]
    public async Task<IReadOnlyList<StudentCourse>> GetCoursesAsync(int studentId, CancellationToken ct = default)
    {
        var student = await RequireStudentAsync(studentId, ct);

        return await db.Courses
            .AsNoTracking()
            .Where(c => c.GradeLevel == student.GradeLevel && c.Section == student.Section)
            .OrderBy(c => c.Name)
            .Select(c => new StudentCourse(c.Id, c.Name, c.Teacher.FullName, c.Color))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    [Tracing(SegmentName = "Student grades")]
    public async Task<StudentGradesResponse> GetGradesAsync(
        int studentId, string? term, CancellationToken ct = default)
    {
        var student = await RequireStudentAsync(studentId, ct);
        term ??= await LatestTermAsync(studentId, ct);

        var courses = await CoursesWithGradeAsync(student, term, studentId, ct);

        return new StudentGradesResponse(
            student.GradeLevel,
            student.Section,
            term ?? string.Empty,
            courses.Select(c => new StudentCourseGrade(c.CourseId, c.Name, c.TeacherName, c.Average ?? 0m)).ToList());
    }

    [Tracing(SegmentName = "Student attendance")]
    public async Task<StudentAttendanceResponse> GetAttendanceAsync(int studentId, CancellationToken ct = default)
    {
        await RequireStudentAsync(studentId, ct);

        var history = await db.Attendance
            .AsNoTracking()
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.Date)
            .Select(a => new StudentAttendanceItem(a.Date, a.Course.Name, a.Present))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var absences = history.Count(h => !h.Present);

        return new StudentAttendanceResponse(absences, history);
    }

    [Tracing(SegmentName = "Student schedule")]
    public async Task<StudentScheduleResponse> GetScheduleAsync(int studentId, CancellationToken ct = default)
    {
        var student = await RequireStudentAsync(studentId, ct);

        // Times are formatted in memory: SQL Server cannot translate ToString("HH:mm").
        var rows = await db.ScheduleSlots
            .AsNoTracking()
            .Where(s => s.Course.GradeLevel == student.GradeLevel && s.Course.Section == student.Section)
            .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
            .Select(s => new
            {
                s.Id,
                s.Course.Name,
                s.Course.GradeLevel,
                s.Course.Section,
                s.StartTime,
                s.EndTime,
                s.DayOfWeek,
                s.Icon,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var classes = rows
            .Select(r => new ScheduledClass(
                r.Id, r.Name, r.GradeLevel, r.Section,
                r.StartTime.ToString("HH:mm"), r.EndTime.ToString("HH:mm"), r.DayOfWeek, r.Icon))
            .ToList();

        return new StudentScheduleResponse(student.GradeLevel, student.Section, classes);
    }

    [Tracing(SegmentName = "Student profile")]
    public async Task<StudentProfileResponse?> GetProfileAsync(int studentId, CancellationToken ct = default)
    {
        return await db.Students
            .AsNoTracking()
            .Where(s => s.Id == studentId)
            .Select(s => new StudentProfileResponse(
                s.Id, s.Name, s.GradeLevel, s.Section, s.Email, s.Phone,
                s.EmailNotifications, s.AppNotifications))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    [Tracing(SegmentName = "Update student profile")]
    public async Task<StudentProfileResponse> UpdateProfileAsync(
        int studentId, UpdateStudentProfileRequest request, CancellationToken ct = default)
    {
        var student = await db.Students
            .FirstOrDefaultAsync(s => s.Id == studentId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Student {studentId} does not exist.");

        student.Email = request.Email;
        student.Phone = request.Phone;
        student.EmailNotifications = request.EmailNotifications;
        student.AppNotifications = request.AppNotifications;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new StudentProfileResponse(
            student.Id, student.Name, student.GradeLevel, student.Section,
            student.Email, student.Phone, student.EmailNotifications, student.AppNotifications);
    }

    /// <summary>Courses of the student's section, each with its grade for the term (null if ungraded).</summary>
    private async Task<List<CourseWithGrade>> CoursesWithGradeAsync(
        Student student, string? term, int studentId, CancellationToken ct)
    {
        return await db.Courses
            .AsNoTracking()
            .Where(c => c.GradeLevel == student.GradeLevel && c.Section == student.Section)
            .OrderBy(c => c.Name)
            .Select(c => new CourseWithGrade(
                c.Id,
                c.Name,
                c.Teacher.FullName,
                db.Grades
                    .Where(g => g.CourseId == c.Id && g.StudentId == studentId && g.Term == term)
                    .Select(g => (decimal?)g.Average)
                    .FirstOrDefault()))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private sealed record CourseWithGrade(int CourseId, string Name, string TeacherName, decimal? Average);
}
