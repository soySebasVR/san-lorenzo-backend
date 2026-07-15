using AWS.Lambda.Powertools.Tracing;
using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Data;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Infrastructure;

namespace ServerlessAPI.Repositories;

public sealed class GradeRepository(SanLorenzoDbContext db) : IGradeRepository
{
    [Tracing(SegmentName = "List grades")]
    public async Task<GradesResponse> GetGradesAsync(
        int teacherId, string course, string gradeLevel, string section, string term,
        CancellationToken ct = default)
    {
        // LEFT JOIN: students with no grades yet still show up, with zeros.
        var entries = await (
                from s in db.Students.AsNoTracking()
                join c in db.Courses.AsNoTracking() on s.CourseId equals c.Id
                where c.TeacherId == teacherId
                      && c.Name == course
                      && c.GradeLevel == gradeLevel
                      && c.Section == section
                join g in db.Grades.AsNoTracking().Where(g => g.Term == term)
                    on new { StudentId = s.Id, CourseId = c.Id } equals new { g.StudentId, g.CourseId }
                    into termGrades
                from g in termGrades.DefaultIfEmpty()
                orderby s.Name
                select new StudentGrade(
                    s.Id,
                    s.Name,
                    g == null ? 0m : g.Score1,
                    g == null ? 0m : g.Score2,
                    g == null ? 0m : g.Score3,
                    g == null ? 0m : g.Score4,
                    g == null ? 0m : g.Score5,
                    g == null ? 0m : g.Average))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new GradesResponse(course, gradeLevel, section, term, entries);
    }

    [Tracing(SegmentName = "Save grade")]
    public async Task UpsertGradeAsync(
        int studentId, int teacherId, UpdateGradeRequest request, CancellationToken ct = default)
    {
        // Both checks at once: the course belongs to this teacher AND the student belongs
        // to that course. Checking only the former lets a teacher write grades for a
        // student of another course by passing an arbitrary studentId.
        var studentBelongsToCourse = await db.Students
            .AsNoTracking()
            .AnyAsync(
                s => s.Id == studentId
                     && s.CourseId == request.CourseId
                     && s.Course.TeacherId == teacherId,
                ct)
            .ConfigureAwait(false);

        if (!studentBelongsToCourse)
            throw new ForbiddenException("Student or course does not belong to this teacher.");

        var average = Math.Round(
            (request.Score1 + request.Score2 + request.Score3 + request.Score4 + request.Score5) / 5m,
            2,
            MidpointRounding.AwayFromZero);

        var grade = await db.Grades
            .FirstOrDefaultAsync(
                g => g.StudentId == studentId
                     && g.CourseId == request.CourseId
                     && g.Term == request.Term,
                ct)
            .ConfigureAwait(false);

        if (grade is null)
        {
            grade = new Grade
            {
                StudentId = studentId,
                CourseId = request.CourseId,
                Term = request.Term,
            };
            db.Grades.Add(grade);
        }

        grade.Score1 = request.Score1;
        grade.Score2 = request.Score2;
        grade.Score3 = request.Score3;
        grade.Score4 = request.Score4;
        grade.Score5 = request.Score5;
        grade.Average = average;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
