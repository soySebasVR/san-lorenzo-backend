using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Infrastructure;
using ServerlessAPI.Repositories;
using Xunit;

namespace ServerlessAPI.Tests;

public sealed class CoordinatorReportRepositoryTests : SqliteTestBase
{
    private const int CoordinatorUserId = 1;
    private const int OtherCoordinatorUserId = 6;
    private const int CourseId = 100;

    private static readonly DateOnly ReportDate =
        new(2026, 7, 10);

    protected override Task SeedAsync()
    {
        Db.Teachers.Add(
            new Teacher
            {
                Id = 10,
                FullName = "Docente de Matemática",
                Email = "docente@sanlorenzo.edu.pe",
                Position = "Docente",
                Subjects = "Matemática",
                EmailNotifications = true,
                AppNotifications = true,
            });

        Db.Courses.Add(
            new Course
            {
                Id = CourseId,
                TeacherId = 10,
                Name = "Matemática",
                GradeLevel = "5to",
                Section = "A",
                Color = "#336699",
                ScheduleText = "Lun y Mié 08:00-09:00",
            });

        Db.Students.AddRange(
            new Student
            {
                Id = 1000,
                CourseId = CourseId,
                Name = "Ana Torres",
            },
            new Student
            {
                Id = 1001,
                CourseId = CourseId,
                Name = "Bruno Pérez",
            });

        Db.Users.AddRange(
            new User
            {
                Id = CoordinatorUserId,
                Email = "coordinador@sanlorenzo.edu.pe",
                FullName = "Coordinador Principal",
                PasswordHash = "hash-prueba",
                Role = Role.Coordinator,
                IsActive = true,
            },
            new User
            {
                Id = OtherCoordinatorUserId,
                Email = "otro.coordinador@sanlorenzo.edu.pe",
                FullName = "Segundo Coordinador",
                PasswordHash = "hash-prueba",
                Role = Role.Coordinator,
                IsActive = true,
            },
            new User
            {
                Id = 2,
                Email = "docente.usuario@sanlorenzo.edu.pe",
                FullName = "Docente de Matemática",
                PasswordHash = "hash-prueba",
                Role = Role.Teacher,
                IsActive = true,
                TeacherId = 10,
            },
            new User
            {
                Id = 3,
                Email = "ana@sanlorenzo.edu.pe",
                FullName = "Ana Torres",
                PasswordHash = "hash-prueba",
                Role = Role.Student,
                IsActive = true,
                StudentId = 1000,
            },
            new User
            {
                Id = 4,
                Email = "bruno@sanlorenzo.edu.pe",
                FullName = "Bruno Pérez",
                PasswordHash = "hash-prueba",
                Role = Role.Student,
                IsActive = false,
                StudentId = 1001,
            });

        Db.Attendance.AddRange(
            new Attendance
            {
                Id = 2000,
                StudentId = 1000,
                CourseId = CourseId,
                Date = ReportDate,
                Present = true,
            },
            new Attendance
            {
                Id = 2001,
                StudentId = 1001,
                CourseId = CourseId,
                Date = ReportDate,
                Present = false,
            });

        Db.Grades.AddRange(
            new Grade
            {
                Id = 3000,
                StudentId = 1000,
                CourseId = CourseId,
                Term = "Bimestre 1",
                Score1 = 14,
                Score2 = 15,
                Score3 = 16,
                Score4 = 15,
                Score5 = 15,
                Average = 15,
            },
            new Grade
            {
                Id = 3001,
                StudentId = 1001,
                CourseId = CourseId,
                Term = "Bimestre 1",
                Score1 = 8,
                Score2 = 9,
                Score3 = 10,
                Score4 = 9,
                Score5 = 9,
                Average = 9,
            });

        return Task.CompletedTask;
    }

    [Fact]
    public async Task GenerateAsync_creates_attendance_report()
    {
        var repository = new CoordinatorReportRepository(Db);

        var response = await repository.GenerateAsync(
            CoordinatorUserId,
            new GenerateCoordinatorReportRequest
            {
                ReportType = "Attendance",
                StartDate = ReportDate,
                EndDate = ReportDate,
                CourseId = CourseId,
            },
            Ct);

        Assert.True(response.Id > 0);
        Assert.Equal("Attendance", response.ReportType);

        var rows = response.Result
            .EnumerateArray()
            .ToArray();

        var row = Assert.Single(rows);

        Assert.Equal(
            "Matemática",
            row.GetProperty("courseName").GetString());

        Assert.Equal(
            1,
            row.GetProperty("presentCount").GetInt32());

        Assert.Equal(
            1,
            row.GetProperty("absentCount").GetInt32());

        Assert.Equal(
            2,
            row.GetProperty("totalCount").GetInt32());

        var saved = await Db.CoordinatorReports
            .AsNoTracking()
            .SingleAsync(Ct);

        Assert.Equal(
            CoordinatorReportType.Attendance,
            saved.ReportType);

        Assert.Equal(
            CoordinatorUserId,
            saved.GeneratedByUserId);
    }

    [Fact]
    public async Task GenerateAsync_creates_grades_report()
    {
        var repository = new CoordinatorReportRepository(Db);

        var response = await repository.GenerateAsync(
            CoordinatorUserId,
            new GenerateCoordinatorReportRequest
            {
                ReportType = "Grades",
                CourseId = CourseId,
                Term = "Bimestre 1",
            },
            Ct);

        Assert.Equal("Grades", response.ReportType);

        var rows = response.Result
            .EnumerateArray()
            .ToArray();

        var row = Assert.Single(rows);

        Assert.Equal(
            2,
            row.GetProperty("studentCount").GetInt32());

        Assert.Equal(
            12m,
            row.GetProperty("averageGrade").GetDecimal());

        Assert.Equal(
            1,
            row.GetProperty("passedCount").GetInt32());

        Assert.Equal(
            1,
            row.GetProperty("failedCount").GetInt32());
    }

    [Fact]
    public async Task GenerateAsync_creates_users_report_grouped_by_role()
    {
        var repository = new CoordinatorReportRepository(Db);

        var response = await repository.GenerateAsync(
            CoordinatorUserId,
            new GenerateCoordinatorReportRequest
            {
                ReportType = "Users",
                UserRole = "Student",
            },
            Ct);

        Assert.Equal("Users", response.ReportType);

        var rows = response.Result
            .EnumerateArray()
            .ToArray();

        var row = Assert.Single(rows);

        Assert.Equal(
            "Student",
            row.GetProperty("role").GetString());

        Assert.Equal(
            1,
            row.GetProperty("activeCount").GetInt32());

        Assert.Equal(
            1,
            row.GetProperty("inactiveCount").GetInt32());

        Assert.Equal(
            2,
            row.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task GenerateAsync_rejects_filters_not_allowed_for_report_type()
    {
        var repository = new CoordinatorReportRepository(Db);

        var request = new GenerateCoordinatorReportRequest
        {
            ReportType = "Attendance",
            Term = "Bimestre 1",
        };

        await Assert.ThrowsAsync<BadRequestException>(
            () => repository.GenerateAsync(
                CoordinatorUserId,
                request,
                Ct));

        Assert.Empty(
            await Db.CoordinatorReports
                .AsNoTracking()
                .ToListAsync(Ct));
    }

    [Fact]
    public async Task GetAsync_filters_reports_by_owner_and_type()
    {
        var repository = new CoordinatorReportRepository(Db);

        await repository.GenerateAsync(
            CoordinatorUserId,
            new GenerateCoordinatorReportRequest
            {
                ReportType = "Attendance",
                StartDate = ReportDate,
                EndDate = ReportDate,
            },
            Ct);

        await repository.GenerateAsync(
            CoordinatorUserId,
            new GenerateCoordinatorReportRequest
            {
                ReportType = "Users",
            },
            Ct);

        await repository.GenerateAsync(
            OtherCoordinatorUserId,
            new GenerateCoordinatorReportRequest
            {
                ReportType = "Users",
            },
            Ct);

        var result = await repository.GetAsync(
            CoordinatorUserId,
            "Users",
            Ct);

        var report = Assert.Single(result);

        Assert.Equal("Users", report.ReportType);
        Assert.Equal(
            CoordinatorUserId,
            report.GeneratedByUserId);
    }

    [Fact]
    public async Task GetByIdAsync_does_not_return_another_coordinators_report()
    {
        var repository = new CoordinatorReportRepository(Db);

        var created = await repository.GenerateAsync(
            CoordinatorUserId,
            new GenerateCoordinatorReportRequest
            {
                ReportType = "Users",
            },
            Ct);

        Db.ChangeTracker.Clear();

        var ownerResult = await repository.GetByIdAsync(
            CoordinatorUserId,
            created.Id,
            Ct);

        var otherCoordinatorResult = await repository.GetByIdAsync(
            OtherCoordinatorUserId,
            created.Id,
            Ct);

        Assert.NotNull(ownerResult);
        Assert.Null(otherCoordinatorResult);
    }
}
