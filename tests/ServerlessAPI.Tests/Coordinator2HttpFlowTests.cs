using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServerlessAPI.Data;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using Xunit;

namespace ServerlessAPI.Tests;

public sealed class Coordinator2HttpFlowTests : IAsyncLifetime
{
    private const string TeacherPassword = "Docente#2026";
    private const string CoordinatorPassword = "Coord#2026";

    private static readonly DateOnly ReportDate =
        new(2026, 7, 10);

    private SqliteConnection _connection = null!;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    private static CancellationToken Ct =>
        TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");

        await _connection.OpenAsync(Ct);

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host =>
            {
                host.UseEnvironment("Development");

                host.ConfigureServices(services =>
                {
                    var efRegistrations = services
                        .Where(d =>
                            d.ServiceType.Namespace?
                                .StartsWith(
                                    "Microsoft.EntityFrameworkCore") == true
                            || d.ServiceType ==
                                typeof(SanLorenzoDbContext)
                            || d.ServiceType ==
                                typeof(
                                    DbContextOptions<
                                        SanLorenzoDbContext>))
                        .ToList();

                    foreach (var registration in efRegistrations)
                        services.Remove(registration);

                    services.AddDbContext<SanLorenzoDbContext>(
                        options =>
                            options.UseSqlite(_connection));
                });
            });

        _client = _factory.CreateClient();

        await SeedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();

        await _factory.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private async Task SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<SanLorenzoDbContext>();

        var hasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher<User>>();

        await db.Database.EnsureCreatedAsync(Ct);

        db.Teachers.Add(
            new Teacher
            {
                Id = 1,
                FullName = "Ana Torres",
                Email = "ana@sl.edu",
                Position = "Docente",
                Subjects = "Matemática",
                EmailNotifications = true,
                AppNotifications = true,
            });

        db.Courses.Add(
            new Course
            {
                Id = 1,
                TeacherId = 1,
                Name = "Matemática",
                GradeLevel = "5to",
                Section = "A",
                Color = "#3B82F6",
                ScheduleText = "Lunes 08:00",
            });

        db.Students.AddRange(
            new Student
            {
                Id = 1,
                CourseId = 1,
                Name = "María López",
            },
            new Student
            {
                Id = 2,
                CourseId = 1,
                Name = "Carlos Pérez",
            });

        var teacher = new User
        {
            Id = 1,
            Email = "teacher@sl.edu",
            FullName = "Ana Torres",
            Role = Role.Teacher,
            IsActive = true,
            TeacherId = 1,
        };

        teacher.PasswordHash =
            hasher.HashPassword(teacher, TeacherPassword);

        var coordinator = new User
        {
            Id = 2,
            Email = "coordinator@sl.edu",
            FullName = "Luis Paz",
            Role = Role.Coordinator,
            IsActive = true,
        };

        coordinator.PasswordHash =
            hasher.HashPassword(
                coordinator,
                CoordinatorPassword);

        var student = new User
        {
            Id = 3,
            Email = "student@sl.edu",
            FullName = "María López",
            Role = Role.Student,
            IsActive = true,
            StudentId = 1,
        };

        student.PasswordHash =
            hasher.HashPassword(student, "Alumno#2026");

        db.Users.AddRange(
            teacher,
            coordinator,
            student);

        db.Attendance.AddRange(
            new Attendance
            {
                Id = 1,
                StudentId = 1,
                CourseId = 1,
                Date = ReportDate,
                Present = true,
            },
            new Attendance
            {
                Id = 2,
                StudentId = 2,
                CourseId = 1,
                Date = ReportDate,
                Present = false,
            });

        db.Grades.AddRange(
            new Grade
            {
                Id = 1,
                StudentId = 1,
                CourseId = 1,
                Term = "Bimestre 1",
                Score1 = 15,
                Score2 = 15,
                Score3 = 15,
                Score4 = 15,
                Score5 = 15,
                Average = 15,
            },
            new Grade
            {
                Id = 2,
                StudentId = 2,
                CourseId = 1,
                Term = "Bimestre 1",
                Score1 = 9,
                Score2 = 9,
                Score3 = 9,
                Score4 = 9,
                Score5 = 9,
                Average = 9,
            });

        await db.SaveChangesAsync(Ct);
    }

    private async Task<string> LoginAsync(
        string email,
        string password)
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                email,
                password,
            },
            Ct);

        response.EnsureSuccessStatusCode();

        var body = await response.Content
            .ReadFromJsonAsync<LoginResponse>(Ct);

        return body!.Token;
    }

    private static HttpRequestMessage Authenticated(
        HttpMethod method,
        string path,
        string token,
        object? body = null)
    {
        var request = new HttpRequestMessage(method, path);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);

        if (body is not null)
            request.Content = JsonContent.Create(body);

        return request;
    }

    [Fact]
    public async Task Coordinator_endpoints_require_authentication()
    {
        var response = await _client.GetAsync(
            "/coordinador/configuracion",
            Ct);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Teacher_cannot_access_coordinator_endpoints()
    {
        var token = await LoginAsync(
            "teacher@sl.edu",
            TeacherPassword);

        var response = await _client.SendAsync(
            Authenticated(
                HttpMethod.Get,
                "/coordinador/configuracion",
                token),
            Ct);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Coordinator_can_create_and_read_configuration()
    {
        var token = await LoginAsync(
            "coordinator@sl.edu",
            CoordinatorPassword);

        var initialResponse = await _client.SendAsync(
            Authenticated(
                HttpMethod.Get,
                "/coordinador/configuracion",
                token),
            Ct);

        Assert.Equal(
            HttpStatusCode.NotFound,
            initialResponse.StatusCode);

        var updateResponse = await _client.SendAsync(
            Authenticated(
                HttpMethod.Put,
                "/coordinador/configuracion",
                token,
                new
                {
                    institutionName =
                        "Institución Educativa San Lorenzo",
                    academicYear = 2026,
                    academicPeriod = "Año lectivo 2026",
                    attendanceToleranceMinutes = 10,
                    absenceAlertPercentage = 30m,
                    timeZone = "America/Lima",
                }),
            Ct);

        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        var configuration = await updateResponse.Content
            .ReadFromJsonAsync<
                InstitutionalConfigurationResponse>(Ct);

        Assert.NotNull(configuration);
        Assert.Equal(
            "Institución Educativa San Lorenzo",
            configuration.InstitutionName);
        Assert.Equal(2026, configuration.AcademicYear);

        var getResponse = await _client.SendAsync(
            Authenticated(
                HttpMethod.Get,
                "/coordinador/configuracion",
                token),
            Ct);

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);
    }

    [Fact]
    public async Task Coordinator_can_update_their_profile()
    {
        var token = await LoginAsync(
            "coordinator@sl.edu",
            CoordinatorPassword);

        var initialResponse = await _client.SendAsync(
            Authenticated(
                HttpMethod.Get,
                "/coordinador/perfil",
                token),
            Ct);

        Assert.Equal(
            HttpStatusCode.OK,
            initialResponse.StatusCode);

        var updateResponse = await _client.SendAsync(
            Authenticated(
                HttpMethod.Put,
                "/coordinador/perfil",
                token,
                new
                {
                    fullName = "Luis Paz Actualizado",
                    email = "luis.paz@sl.edu",
                    phone = "999888777",
                    managementArea = "Gestión Académica",
                    emailNotifications = true,
                    appNotifications = false,
                }),
            Ct);

        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        var profile = await updateResponse.Content
            .ReadFromJsonAsync<
                CoordinatorProfileResponse>(Ct);

        Assert.NotNull(profile);
        Assert.Equal(
            "Luis Paz Actualizado",
            profile.FullName);
        Assert.Equal(
            "luis.paz@sl.edu",
            profile.Email);
        Assert.Equal(
            "999888777",
            profile.Phone);
        Assert.False(profile.AppNotifications);
    }

    [Fact]
    public async Task Coordinator_can_create_list_and_update_announcement()
    {
        var token = await LoginAsync(
            "coordinator@sl.edu",
            CoordinatorPassword);

        var createResponse = await _client.SendAsync(
            Authenticated(
                HttpMethod.Post,
                "/coordinador/comunicados",
                token,
                new
                {
                    title = "Reunión institucional",
                    content =
                        "Se comunica la reunión institucional.",
                    status = "Draft",
                    recipients = new[]
                    {
                        new
                        {
                            targetType = "All",
                        },
                    },
                }),
            Ct);

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var created = await createResponse.Content
            .ReadFromJsonAsync<
                CoordinatorAnnouncementResponse>(Ct);

        Assert.NotNull(created);
        Assert.Equal("Draft", created.Status);

        var listResponse = await _client.SendAsync(
            Authenticated(
                HttpMethod.Get,
                "/coordinador/comunicados?status=Draft",
                token),
            Ct);

        Assert.Equal(
            HttpStatusCode.OK,
            listResponse.StatusCode);

        var announcements = await listResponse.Content
            .ReadFromJsonAsync<
                List<CoordinatorAnnouncementResponse>>(Ct);

        Assert.Single(announcements!);

        var updateResponse = await _client.SendAsync(
            Authenticated(
                HttpMethod.Put,
                $"/coordinador/comunicados/{created.Id}",
                token,
                new
                {
                    title = "Reunión institucional actualizada",
                    content =
                        "Se comunica la nueva fecha de reunión.",
                    scheduledAt =
                        DateTime.UtcNow.AddDays(1),
                    status = "Scheduled",
                    recipients = new[]
                    {
                        new
                        {
                            targetType = "GradeSection",
                            gradeLevel = "5to",
                            section = "A",
                        },
                    },
                }),
            Ct);

        Assert.Equal(
            HttpStatusCode.OK,
            updateResponse.StatusCode);

        var updated = await updateResponse.Content
            .ReadFromJsonAsync<
                CoordinatorAnnouncementResponse>(Ct);

        Assert.NotNull(updated);
        Assert.Equal("Scheduled", updated.Status);
        Assert.Single(updated.Recipients);
    }

    [Fact]
    public async Task Coordinator_can_generate_list_and_read_report()
    {
        var token = await LoginAsync(
            "coordinator@sl.edu",
            CoordinatorPassword);

        var generateResponse = await _client.SendAsync(
            Authenticated(
                HttpMethod.Post,
                "/coordinador/reportes/generar",
                token,
                new
                {
                    reportType = "Attendance",
                    startDate = ReportDate,
                    endDate = ReportDate,
                    courseId = 1,
                }),
            Ct);

        Assert.Equal(
            HttpStatusCode.Created,
            generateResponse.StatusCode);

        var generated = await generateResponse.Content
            .ReadFromJsonAsync<
                CoordinatorReportResponse>(Ct);

        Assert.NotNull(generated);
        Assert.Equal("Attendance", generated.ReportType);

        var rows = generated.Result
            .EnumerateArray()
            .ToArray();

        var row = Assert.Single(rows);

        Assert.Equal(
            1,
            row.GetProperty("presentCount").GetInt32());

        Assert.Equal(
            1,
            row.GetProperty("absentCount").GetInt32());

        var listResponse = await _client.SendAsync(
            Authenticated(
                HttpMethod.Get,
                "/coordinador/reportes?reportType=Attendance",
                token),
            Ct);

        Assert.Equal(
            HttpStatusCode.OK,
            listResponse.StatusCode);

        var reports = await listResponse.Content
            .ReadFromJsonAsync<
                List<CoordinatorReportSummaryResponse>>(Ct);

        Assert.Single(reports!);

        var detailResponse = await _client.SendAsync(
            Authenticated(
                HttpMethod.Get,
                $"/coordinador/reportes/{generated.Id}",
                token),
            Ct);

        Assert.Equal(
            HttpStatusCode.OK,
            detailResponse.StatusCode);
    }
}
