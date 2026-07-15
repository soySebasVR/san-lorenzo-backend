using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Infrastructure;
using ServerlessAPI.Repositories;
using Xunit;

namespace ServerlessAPI.Tests;

public sealed class GradeRepositoryTests : SqliteTestBase
{
    private const int TeacherId = 1;
    private const int OtherTeacherId = 2;
    private const int CourseId = 10;
    private const int OtherCourseId = 11;
    private const int StudentId = 100;
    private const int OtherStudentId = 101;

    protected override Task SeedAsync()
    {
        Db.Teachers.AddRange(
            new Teacher { Id = TeacherId, FullName = "Ana Torres", Email = "ana@sl.edu", Position = "Docente" },
            new Teacher { Id = OtherTeacherId, FullName = "Luis Paz", Email = "luis@sl.edu", Position = "Docente" });

        Db.Courses.AddRange(
            new Course { Id = CourseId, TeacherId = TeacherId, Name = "Matemática", GradeLevel = "3ro", Section = "A", Color = "#3B82F6" },
            new Course { Id = OtherCourseId, TeacherId = OtherTeacherId, Name = "Historia", GradeLevel = "3ro", Section = "B", Color = "#EF4444" });

        Db.Students.AddRange(
            new Student { Id = StudentId, CourseId = CourseId, Name = "Carlos Ruiz" },
            new Student { Id = OtherStudentId, CourseId = OtherCourseId, Name = "Marta Díaz" });

        return Task.CompletedTask;
    }

    private static UpdateGradeRequest Scores(int courseId, decimal s1, decimal s2, decimal s3, decimal s4, decimal s5) =>
        new() { CourseId = courseId, Term = "2026-I", Score1 = s1, Score2 = s2, Score3 = s3, Score4 = s4, Score5 = s5 };

    [Fact]
    public async Task GetGrades_returns_students_without_grades_as_zeros()
    {
        var repo = new GradeRepository(Db);

        var result = await repo.GetGradesAsync(TeacherId, "Matemática", "3ro", "A", "2026-I", Ct);

        var student = Assert.Single(result.Entries);
        Assert.Equal("Carlos Ruiz", student.Name);
        Assert.Equal(0m, student.Average);
    }

    [Fact]
    public async Task UpsertGrade_inserts_and_computes_the_average()
    {
        var repo = new GradeRepository(Db);

        await repo.UpsertGradeAsync(StudentId, TeacherId, Scores(CourseId, 15, 16, 14, 18, 12), Ct);

        var grade = await Db.Grades.SingleAsync(Ct);
        Assert.Equal(15m, grade.Average); // (15+16+14+18+12) / 5
    }

    [Fact]
    public async Task UpsertGrade_twice_updates_instead_of_duplicating()
    {
        var repo = new GradeRepository(Db);

        await repo.UpsertGradeAsync(StudentId, TeacherId, Scores(CourseId, 10, 10, 10, 10, 10), Ct);
        await repo.UpsertGradeAsync(StudentId, TeacherId, Scores(CourseId, 20, 10, 10, 10, 10), Ct);

        var grade = await Db.Grades.SingleAsync(Ct);
        Assert.Equal(20m, grade.Score1);
        Assert.Equal(12m, grade.Average); // (20+10+10+10+10) / 5
    }

    [Fact]
    public async Task UpsertGrade_rejects_a_course_owned_by_another_teacher()
    {
        var repo = new GradeRepository(Db);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => repo.UpsertGradeAsync(OtherStudentId, TeacherId, Scores(OtherCourseId, 20, 20, 20, 20, 20), Ct));
    }

    /// <summary>
    /// The hole the Python version had: the course did belong to the teacher, but the
    /// student belonged to a different course, and the grade got written anyway.
    /// </summary>
    [Fact]
    public async Task UpsertGrade_rejects_a_student_from_another_course()
    {
        var repo = new GradeRepository(Db);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => repo.UpsertGradeAsync(OtherStudentId, TeacherId, Scores(CourseId, 20, 20, 20, 20, 20), Ct));

        Assert.Empty(await Db.Grades.ToListAsync(Ct));
    }
}
