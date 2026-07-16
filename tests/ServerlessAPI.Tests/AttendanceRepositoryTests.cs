using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Infrastructure;
using ServerlessAPI.Repositories;
using Xunit;

namespace ServerlessAPI.Tests;

public sealed class AttendanceRepositoryTests : SqliteTestBase
{
    private const int TeacherId = 1;
    private const int CourseId = 10;
    private const int StudentA = 100;
    private const int StudentB = 101;

    private static readonly DateOnly Today = new(2026, 7, 14);

    protected override Task SeedAsync()
    {
        Db.Teachers.Add(new Teacher
        {
            Id = TeacherId, FullName = "Ana Torres", Email = "ana@sl.edu", Position = "Docente",
        });
        Db.Courses.Add(new Course
        {
            Id = CourseId, TeacherId = TeacherId, Name = "Matemática",
            GradeLevel = "3ro", Section = "A", Color = "#3B82F6",
        });
        Db.Students.AddRange(
            new Student { Id = StudentA, GradeLevel = "3ro", Section = "A", Name = "Ana Alumna" },
            new Student { Id = StudentB, GradeLevel = "3ro", Section = "A", Name = "Beto Alumno" });

        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetAttendance_with_no_records_treats_everyone_as_present()
    {
        var repo = new AttendanceRepository(Db);

        var result = await repo.GetAttendanceAsync(TeacherId, CourseId, Today, Ct);

        Assert.Equal("Matemática", result.Course);
        Assert.Equal(2, result.Students.Count);
        Assert.All(result.Students, s => Assert.True(s.Present));
    }

    [Fact]
    public async Task SaveAttendance_inserts_then_corrects_without_duplicating()
    {
        var repo = new AttendanceRepository(Db);

        await repo.SaveAttendanceAsync(TeacherId, new SaveAttendanceRequest
        {
            CourseId = CourseId,
            Date = Today,
            Entries = [
                new AttendanceEntry { StudentId = StudentA, Present = true },
                new AttendanceEntry { StudentId = StudentB, Present = false },
            ],
        }, Ct);

        await repo.SaveAttendanceAsync(TeacherId, new SaveAttendanceRequest
        {
            CourseId = CourseId,
            Date = Today,
            Entries = [new AttendanceEntry { StudentId = StudentB, Present = true }],
        }, Ct);

        var records = await Db.Attendance.AsNoTracking().ToListAsync(Ct);
        Assert.Equal(2, records.Count);
        Assert.True(records.Single(r => r.StudentId == StudentB).Present);
    }

    [Fact]
    public async Task SaveAttendance_rejects_a_student_from_another_course()
    {
        var repo = new AttendanceRepository(Db);

        var request = new SaveAttendanceRequest
        {
            CourseId = CourseId,
            Date = Today,
            Entries = [new AttendanceEntry { StudentId = 999, Present = true }],
        };

        await Assert.ThrowsAsync<ForbiddenException>(
            () => repo.SaveAttendanceAsync(TeacherId, request, Ct));
    }

    [Fact]
    public async Task GetAttendance_of_another_teachers_course_does_not_reveal_it_exists()
    {
        var repo = new AttendanceRepository(Db);

        await Assert.ThrowsAsync<NotFoundException>(
            () => repo.GetAttendanceAsync(teacherId: 999, CourseId, Today, Ct));
    }
}
