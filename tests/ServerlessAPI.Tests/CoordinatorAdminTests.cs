using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Infrastructure;
using ServerlessAPI.Repositories;
using Xunit;

namespace ServerlessAPI.Tests;

public sealed class CoordinatorAdminTests : SqliteTestBase
{
    private static readonly PasswordHasher<User> Hasher = new();

    protected override Task SeedAsync()
    {
        Db.Teachers.Add(new Teacher { Id = 1, FullName = "Ana Torres", Email = "ana@sl.edu", Position = "Docente" });
        Db.Courses.Add(new Course { Id = 10, TeacherId = 1, Name = "Matemática", GradeLevel = "3ro", Section = "A", Color = "#3B82F6" });
        Db.Students.Add(new Student { Id = 100, Name = "Carlos", GradeLevel = "3ro", Section = "A" });

        var coordinator = new User
        {
            Id = 1, Email = "coord@sl.edu", FullName = "Coord", Role = Role.Coordinator, IsActive = true,
        };
        coordinator.PasswordHash = Hasher.HashPassword(coordinator, "Coord#2026");
        Db.Users.Add(coordinator);

        return Task.CompletedTask;
    }

    // ── Users ────────────────────────────────────────────────────────────────
    [Fact]
    public async Task CreateUser_coordinator_needs_no_profile()
    {
        var repo = new UserAdminRepository(Db, Hasher);

        var created = await repo.CreateAsync(new CreateUserRequest
        {
            Email = "new-coord@sl.edu", FullName = "New Coord", Password = "Secret#2026", Role = "Coordinator",
        }, Ct);

        Assert.Equal("Coordinator", created.Role);
        Assert.Null(created.TeacherId);
    }

    [Fact]
    public async Task CreateUser_teacher_requires_an_existing_teacherId()
    {
        var repo = new UserAdminRepository(Db, Hasher);

        await Assert.ThrowsAsync<ForbiddenException>(() => repo.CreateAsync(new CreateUserRequest
        {
            Email = "t@sl.edu", FullName = "T", Password = "Secret#2026", Role = "Teacher", TeacherId = 999,
        }, Ct));
    }

    [Fact]
    public async Task CreateUser_rejects_a_duplicate_email()
    {
        var repo = new UserAdminRepository(Db, Hasher);

        await Assert.ThrowsAsync<ForbiddenException>(() => repo.CreateAsync(new CreateUserRequest
        {
            Email = "coord@sl.edu", FullName = "Dup", Password = "Secret#2026", Role = "Coordinator",
        }, Ct));
    }

    [Fact]
    public async Task UpdateUser_can_deactivate()
    {
        var repo = new UserAdminRepository(Db, Hasher);

        var updated = await repo.UpdateAsync(1, new UpdateUserRequest { FullName = "Coord", IsActive = false }, Ct);

        Assert.NotNull(updated);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task ListUsers_search_matches_name_or_email()
    {
        var repo = new UserAdminRepository(Db, Hasher);

        var byEmail = await repo.ListAsync("coord@sl", Ct);
        Assert.Single(byEmail);
    }

    // ── Courses ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task CreateCourse_requires_an_existing_teacher()
    {
        var repo = new CourseAdminRepository(Db);

        await Assert.ThrowsAsync<ForbiddenException>(() => repo.CreateAsync(new SaveCourseRequest
        {
            Name = "Ciencias", GradeLevel = "3ro", Section = "A", TeacherId = 999, Color = "#10B981",
        }, Ct));
    }

    [Fact]
    public async Task CreateCourse_then_it_shows_in_the_list()
    {
        var repo = new CourseAdminRepository(Db);

        await repo.CreateAsync(new SaveCourseRequest
        {
            Name = "Ciencias", GradeLevel = "3ro", Section = "A", TeacherId = 1, Color = "#10B981",
        }, Ct);

        var list = await repo.ListAsync(Ct);
        Assert.Equal(2, list.Count);
        Assert.Contains(list, c => c.Name == "Ciencias" && c.TeacherName == "Ana Torres");
    }

    [Fact]
    public async Task DeleteCourse_refuses_when_it_has_grades()
    {
        Db.Grades.Add(new Grade { StudentId = 100, CourseId = 10, Term = "2026-I", Average = 15 });
        await Db.SaveChangesAsync(Ct);

        var repo = new CourseAdminRepository(Db);

        await Assert.ThrowsAsync<ForbiddenException>(() => repo.DeleteAsync(10, Ct));
    }

    [Fact]
    public async Task GetGradeSections_returns_distinct_combinations()
    {
        var repo = new CourseAdminRepository(Db);

        var sections = await repo.GetGradeSectionsAsync(Ct);

        Assert.Single(sections);
        Assert.Equal(("3ro", "A"), (sections[0].GradeLevel, sections[0].Section));
    }

    // ── Settings ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task Settings_get_creates_a_default_then_update_persists()
    {
        var repo = new SettingsRepository(Db);

        var initial = await repo.GetAsync(Ct);
        Assert.False(string.IsNullOrWhiteSpace(initial.SchoolName));

        var updated = await repo.UpdateAsync(new UpdateSettingsRequest
        {
            SchoolName = "IEP Santiago", AcademicYear = 2026, CurrentTerm = "2026-II",
            UnjustifiedAbsenceThreshold = 5, LatenessToleranceMinutes = 15,
        }, Ct);

        Assert.Equal("2026-II", updated.CurrentTerm);
        Assert.Equal(1, await Db.SystemSettings.CountAsync(Ct)); // still a single row
    }

    // ── Dashboard ────────────────────────────────────────────────────────────
    [Fact]
    public async Task Dashboard_counts_students_and_active_teachers()
    {
        var teacherUser = new User
        {
            Id = 2, Email = "ana@sl.edu", FullName = "Ana", Role = Role.Teacher, IsActive = true, TeacherId = 1,
        };
        teacherUser.PasswordHash = Hasher.HashPassword(teacherUser, "x-Secret-2026");
        Db.Users.Add(teacherUser);
        await Db.SaveChangesAsync(Ct);

        var dash = await new CoordinatorDashboardRepository(Db).GetDashboardAsync(Ct);

        Assert.Equal(1, dash.TotalStudents);
        Assert.Equal(1, dash.ActiveTeachers);
    }
}
