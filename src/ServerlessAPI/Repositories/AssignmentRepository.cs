using AWS.Lambda.Powertools.Tracing;
using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Data;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Infrastructure;

namespace ServerlessAPI.Repositories;

public sealed class AssignmentRepository(SanLorenzoDbContext db) : IAssignmentRepository
{
    [Tracing(SegmentName = "Create assignment")]
    public async Task<TeacherAssignment> CreateAsync(
        int teacherId, CreateAssignmentRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<AssignmentType>(request.Type, ignoreCase: true, out var type))
            throw new ForbiddenException($"Unknown assignment type '{request.Type}'.");

        if (request.DueDate < request.StartDate)
            throw new ForbiddenException("Due date must not be before the start date.");

        var course = await db.Courses
            .AsNoTracking()
            .Where(c => c.Id == request.CourseId && c.TeacherId == teacherId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (course is null)
            throw new ForbiddenException("Course does not belong to this teacher.");

        var assignment = new Assignment
        {
            CourseId = request.CourseId,
            Title = request.Title,
            Type = type,
            StartDate = request.StartDate,
            DueDate = request.DueDate,
        };

        db.Assignments.Add(assignment);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new TeacherAssignment(
            assignment.Id, assignment.CourseId, course, assignment.Title,
            assignment.Type.ToString(), assignment.StartDate, assignment.DueDate);
    }

    [Tracing(SegmentName = "List teacher assignments")]
    public async Task<IReadOnlyList<TeacherAssignment>> ListForTeacherAsync(
        int teacherId, int? courseId, CancellationToken ct = default)
    {
        var query = db.Assignments
            .AsNoTracking()
            .Where(a => a.Course.TeacherId == teacherId);

        if (courseId is { } id)
            query = query.Where(a => a.CourseId == id);

        return await query
            .OrderBy(a => a.DueDate)
            .Select(a => new TeacherAssignment(
                a.Id, a.CourseId, a.Course.Name, a.Title, a.Type.ToString(), a.StartDate, a.DueDate))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    [Tracing(SegmentName = "Delete assignment")]
    public async Task<bool> DeleteAsync(int teacherId, int assignmentId, CancellationToken ct = default)
    {
        var assignment = await db.Assignments
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.Course.TeacherId == teacherId, ct)
            .ConfigureAwait(false);

        if (assignment is null)
            return false;

        db.Assignments.Remove(assignment);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    [Tracing(SegmentName = "List student assignments")]
    public async Task<IReadOnlyList<StudentAssignment>> ListForStudentAsync(
        int studentId, string? type, CancellationToken ct = default)
    {
        var student = await db.Students
            .AsNoTracking()
            .Where(s => s.Id == studentId)
            .Select(s => new { s.GradeLevel, s.Section })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Student {studentId} does not exist.");

        var query = db.Assignments
            .AsNoTracking()
            .Where(a => a.Course.GradeLevel == student.GradeLevel && a.Course.Section == student.Section);

        if (!string.IsNullOrWhiteSpace(type)
            && Enum.TryParse<AssignmentType>(type, ignoreCase: true, out var parsed))
        {
            query = query.Where(a => a.Type == parsed);
        }

        var rows = await query
            .OrderBy(a => a.DueDate)
            .Select(a => new { a.Id, a.Course.Name, a.Title, a.Type, a.StartDate, a.DueDate })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return rows
            .Select(a => new StudentAssignment(
                a.Id, a.Name, a.Title, a.Type.ToString(), a.StartDate, a.DueDate,
                a.DueDate < today ? "vencida" : "pendiente"))
            .ToList();
    }
}
