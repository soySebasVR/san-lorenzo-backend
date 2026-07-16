using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Infrastructure;
using ServerlessAPI.Repositories;
using Xunit;

namespace ServerlessAPI.Tests;

public sealed class RemainingFeaturesTests : SqliteTestBase
{
    private const int TeacherId = 1;
    private const int MathId = 10;
    private const int Carlos = 100;

    protected override Task SeedAsync()
    {
        Db.Teachers.Add(new Teacher { Id = TeacherId, FullName = "Ana Torres", Email = "ana@sl.edu", Position = "Docente" });
        Db.Courses.Add(new Course { Id = MathId, TeacherId = TeacherId, Name = "Matemática", GradeLevel = "3ro", Section = "A", Color = "#3B82F6" });
        Db.Students.AddRange(
            new Student { Id = Carlos, Name = "Carlos Ruiz", GradeLevel = "3ro", Section = "A" },
            new Student { Id = 101, Name = "Lucía Paz", GradeLevel = "3ro", Section = "A" });
        Db.Grades.Add(new Grade
        {
            StudentId = Carlos, CourseId = MathId, Term = "2026-I",
            Score1 = 16, Score2 = 16, Score3 = 16, Score4 = 16, Score5 = 16, Average = 16,
        });
        return Task.CompletedTask;
    }

    // ── Assignments ──────────────────────────────────────────────────────────
    [Fact]
    public async Task CreateAssignment_rejects_a_course_of_another_teacher()
    {
        var repo = new AssignmentRepository(Db);

        await Assert.ThrowsAsync<ForbiddenException>(() => repo.CreateAsync(teacherId: 999, new CreateAssignmentRequest
        {
            CourseId = MathId, Title = "Tarea 1", Type = "Task",
            StartDate = new DateOnly(2026, 7, 1), DueDate = new DateOnly(2026, 7, 5),
        }, Ct));
    }

    [Fact]
    public async Task Student_sees_section_assignments_with_derived_status()
    {
        var repo = new AssignmentRepository(Db);

        // One overdue, one upcoming (relative to today).
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await repo.CreateAsync(TeacherId, new CreateAssignmentRequest
        {
            CourseId = MathId, Title = "Vieja", Type = "Task",
            StartDate = today.AddDays(-10), DueDate = today.AddDays(-3),
        }, Ct);
        await repo.CreateAsync(TeacherId, new CreateAssignmentRequest
        {
            CourseId = MathId, Title = "Nueva", Type = "Exam",
            StartDate = today, DueDate = today.AddDays(5),
        }, Ct);

        var all = await repo.ListForStudentAsync(Carlos, type: null, Ct);
        Assert.Equal(2, all.Count);
        Assert.Equal("vencida", all.Single(a => a.Title == "Vieja").Status);
        Assert.Equal("pendiente", all.Single(a => a.Title == "Nueva").Status);

        var examsOnly = await repo.ListForStudentAsync(Carlos, type: "Exam", Ct);
        Assert.Single(examsOnly);
        Assert.Equal("Nueva", examsOnly[0].Title);
    }

    // ── Reports ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task Report_generate_then_download_contains_the_averages()
    {
        var repo = new ReportRepository(Db);

        var report = await repo.GenerateAsync(new GenerateReportRequest
        {
            GradeLevel = "3ro", Term = "2026-I",
        }, Ct);

        var list = await repo.ListAsync(Ct);
        Assert.Single(list);

        var text = await repo.BuildTextAsync(report.Id, Ct);

        Assert.NotNull(text);
        Assert.Contains("REPORTE DE PROMEDIOS", text);
        Assert.Contains("Matemática", text);
        Assert.Contains("Carlos Ruiz", text);
        Assert.Contains("16.00", text);                 // Carlos' average
        Assert.Contains("Promedio del curso: 16.00", text); // only graded student counts
    }

    [Fact]
    public async Task Report_download_of_a_missing_id_is_null()
    {
        var text = await new ReportRepository(Db).BuildTextAsync(999, Ct);
        Assert.Null(text);
    }

    // ── Broadcasts & behavior ────────────────────────────────────────────────
    [Fact]
    public async Task Broadcast_create_then_list()
    {
        var repo = new BroadcastRepository(Db);

        await repo.CreateAsync(new CreateBroadcastRequest
        {
            Subject = "Reunión", Body = "Mañana 5pm", Audience = "Parents",
            GradeLevel = "3ro", ScheduledFor = new DateTime(2026, 7, 20, 17, 0, 0, DateTimeKind.Utc),
        }, Ct);

        var list = await repo.ListAsync(Ct);
        Assert.Single(list);
        Assert.Equal("Parents", list[0].Audience);
    }

    [Fact]
    public async Task Behavior_create_requires_an_existing_student()
    {
        var repo = new BehaviorRepository(Db);

        await Assert.ThrowsAsync<NotFoundException>(() => repo.CreateAsync(new CreateBehaviorReportRequest
        {
            StudentId = 999, Date = new DateOnly(2026, 7, 10), Description = "x",
        }, Ct));
    }
}
