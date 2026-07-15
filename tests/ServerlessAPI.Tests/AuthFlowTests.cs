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

/// <summary>
/// End-to-end auth against the real app in memory. The only tests that exercise the full
/// pipeline (authentication middleware, [Authorize(Roles = ...)], claims, repositories) —
/// unit tests would not notice a misplaced [Authorize].
/// </summary>
public sealed class AuthFlowTests : IAsyncLifetime
{
    private const string TeacherPassword = "Docente#2026";
    private const string CoordinatorPassword = "Coord#2026";

    private SqliteConnection _connection = null!;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync(Ct);

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
        {
            // Development makes Program read the JWT key from appsettings.Development.json
            // instead of Secrets Manager.
            host.UseEnvironment("Development");

            host.ConfigureServices(services =>
            {
                // Dropping DbContextOptions is not enough: the SQL Server provider registers
                // dozens of internal services and EF refuses to run with two providers. Sweep
                // every EF registration, then re-add the context on SQLite. Everything else
                // (auth, DI, routing) stays as it is in production — that is the point.
                var efRegistrations = services
                    .Where(d => d.ServiceType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true
                                || d.ServiceType == typeof(SanLorenzoDbContext)
                                || d.ServiceType == typeof(DbContextOptions<SanLorenzoDbContext>))
                    .ToList();

                foreach (var registration in efRegistrations)
                    services.Remove(registration);

                services.AddDbContext<SanLorenzoDbContext>(o => o.UseSqlite(_connection));
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
        var db = scope.ServiceProvider.GetRequiredService<SanLorenzoDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        await db.Database.EnsureCreatedAsync(Ct);

        db.Teachers.Add(new Teacher
        {
            Id = 1, FullName = "Ana Torres", Email = "ana@sl.edu", Position = "Docente",
        });
        db.Courses.Add(new Course
        {
            Id = 1, TeacherId = 1, Name = "Matemática",
            GradeLevel = "3ro", Section = "A", Color = "#3B82F6",
        });

        var teacher = new User
        {
            Id = 1, Email = "teacher@sl.edu", FullName = "Ana Torres",
            Role = Role.Teacher, IsActive = true, TeacherId = 1,
        };
        teacher.PasswordHash = hasher.HashPassword(teacher, TeacherPassword);

        var coordinator = new User
        {
            Id = 2, Email = "coordinator@sl.edu", FullName = "Luis Paz",
            Role = Role.Coordinator, IsActive = true,
        };
        coordinator.PasswordHash = hasher.HashPassword(coordinator, CoordinatorPassword);

        db.Users.AddRange(teacher, coordinator);

        await db.SaveChangesAsync(Ct);
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new { email, password }, Ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(Ct);
        return body!.Token;
    }

    private static HttpRequestMessage Authenticated(HttpMethod method, string path, string token)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    [Fact]
    public async Task Teacher_login_returns_a_token_and_their_role()
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/login",
            new { email = "teacher@sl.edu", password = TeacherPassword },
            Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(Ct);

        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.Token));
        Assert.Equal("Teacher", body.User.Role);
        Assert.Equal(1, body.User.TeacherId);
    }

    [Fact]
    public async Task Login_with_a_wrong_password_returns_401()
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/login",
            new { email = "teacher@sl.edu", password = "not-this-one" },
            Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Without_a_token_teacher_endpoints_return_401()
    {
        var response = await _client.GetAsync("/docente/cursos", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task With_a_teacher_token_their_courses_are_reachable()
    {
        var token = await LoginAsync("teacher@sl.edu", TeacherPassword);

        var response = await _client.SendAsync(
            Authenticated(HttpMethod.Get, "/docente/cursos", token), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var courses = await response.Content.ReadFromJsonAsync<List<CourseResponse>>(Ct);

        Assert.Single(courses!);
        Assert.Equal("Matemática", courses![0].Name);
    }

    /// <summary>
    /// What motivated the whole change: an X-Docente-ID header used to be enough to read
    /// and edit someone else's grades. The role now travels signed inside the token.
    /// </summary>
    [Fact]
    public async Task An_authenticated_coordinator_cannot_reach_teacher_endpoints()
    {
        var token = await LoginAsync("coordinator@sl.edu", CoordinatorPassword);

        var response = await _client.SendAsync(
            Authenticated(HttpMethod.Get, "/docente/cursos", token), Ct);

        // 403, not 401: the coordinator is authenticated, just not allowed.
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task A_tampered_token_is_rejected()
    {
        var token = await LoginAsync("teacher@sl.edu", TeacherPassword);

        var tampered = token[..^2] + (token[^1] == 'A' ? 'B' : 'A');

        var response = await _client.SendAsync(
            Authenticated(HttpMethod.Get, "/docente/cursos", tampered), Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_returns_the_user_from_the_token()
    {
        var token = await LoginAsync("coordinator@sl.edu", CoordinatorPassword);

        var response = await _client.SendAsync(
            Authenticated(HttpMethod.Get, "/auth/me", token), Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var current = await response.Content.ReadFromJsonAsync<CurrentUser>(Ct);

        Assert.Equal("Coordinator", current!.Role);
        Assert.Null(current.TeacherId);
    }
}
