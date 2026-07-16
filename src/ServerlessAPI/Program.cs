using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using AWS.Lambda.Powertools.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using ServerlessAPI.Authentication;
using ServerlessAPI.Data;
using ServerlessAPI.Entities;
using ServerlessAPI.Infrastructure;
using ServerlessAPI.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Powertools Logger implements ILogger, so the app just uses ILogger<T> and still gets
// structured JSON with cold-start and correlation id.
builder.Logging
    .ClearProviders()
    .AddPowertoolsLogger(config =>
    {
        config.Service = "san-lorenzo-app";
        config.LoggerOutputCase = LoggerOutputCase.CamelCase;
    });

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Angular consumes camelCase.
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// ── Secrets ──────────────────────────────────────────────────────────────────
var connectionStrings = new ConnectionStringProvider(builder.Configuration);
var jwtKeys = new JwtKeyProvider(builder.Configuration);

builder.Services.AddSingleton(connectionStrings);
builder.Services.AddSingleton(jwtKeys);

ISecretBackedProvider[] secrets = [connectionStrings, jwtKeys];

// Lambda reports how it is initializing: "on-demand", "provisioned-concurrency" or
// "snap-start". Outside Lambda the variable does not exist.
var isSnapStart = Environment.GetEnvironmentVariable("AWS_LAMBDA_INITIALIZATION_TYPE") == "snap-start";

if (isSnapStart)
{
    // Secrets are not read during INIT: whatever is cached there ends up inside the
    // snapshot, which is reused across every execution environment. Cleared before the
    // snapshot and re-read after each restore, so rotation works on its own.
    Amazon.Lambda.Core.SnapshotRestore.RegisterBeforeSnapshot(() =>
    {
        foreach (var secret in secrets)
            secret.Clear();

        return ValueTask.CompletedTask;
    });

    Amazon.Lambda.Core.SnapshotRestore.RegisterAfterRestore(async () =>
    {
        foreach (var secret in secrets)
            await secret.WarmAsync();
    });
}
else
{
    // Resolved during INIT, where Lambda gives full CPU for free.
    foreach (var secret in secrets)
        await secret.WarmAsync();
}

// ── Authentication ───────────────────────────────────────────────────────────
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtKeys.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtKeys.Audience,

            ValidateLifetime = true,
            // Default is 5 minutes of tolerance, which keeps expired tokens alive.
            ClockSkew = TimeSpan.Zero,

            ValidateIssuerSigningKey = true,
            // A resolver rather than a fixed key: after a SnapStart restore the key is
            // re-read, and validation picks up the new one without rebuilding the host.
            IssuerSigningKeyResolver = (_, _, _, _) => [jwtKeys.Key],

            RoleClaimType = ClaimTypes.Role,
        };
    });

builder.Services.AddAuthorization();

// Identity's hashing primitive (PBKDF2 with salt), used on its own: no Identity tables,
// no DbContext, no proprietary tokens.
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<TokenService>();

// ── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<SanLorenzoDbContext>((sp, options) =>
{
    // Read per scope, not captured at startup, so a SnapStart restore picks up the new
    // connection string.
    var connectionString = sp.GetRequiredService<ConnectionStringProvider>().Value;

    options.UseSqlServer(connectionString, sql =>
    {
        // RDS drops connections on failover and maintenance; retrying keeps that from
        // surfacing as a 500.
        // 258 / -2 are SQL Server timeout errors that hit the first connection from a cold
        // Lambda container; adding them here makes EF retry instead of surfacing a 500.
        sql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(6),
            errorNumbersToAdd: [258, -2]);

        sql.CommandTimeout(30);
    });

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// ── Dependency injection ─────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, UserContext>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<IGradeRepository, GradeRepository>();
builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();
builder.Services.AddScoped<IScheduleRepository, ScheduleRepository>();
builder.Services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
builder.Services.AddScoped<IProfileRepository, ProfileRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();

builder.Services.AddScoped<ICoordinatorDashboardRepository, CoordinatorDashboardRepository>();
builder.Services.AddScoped<IUserAdminRepository, UserAdminRepository>();
builder.Services.AddScoped<ICourseAdminRepository, CourseAdminRepository>();
builder.Services.AddScoped<IScheduleAdminRepository, ScheduleAdminRepository>();
builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();
builder.Services.AddScoped<ICoordinatorProfileRepository, CoordinatorProfileRepository>();
builder.Services.AddScoped<IAssignmentRepository, AssignmentRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IBroadcastRepository, BroadcastRepository>();
builder.Services.AddScoped<IBehaviorRepository, BehaviorRepository>();

// ── CORS ─────────────────────────────────────────────────────────────────────
const string corsPolicy = "san-lorenzo-frontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? ["http://localhost:4200"];

builder.Services.AddCors(options =>
    options.AddPolicy(corsPolicy, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

// AddDbContextCheck runs CanConnectAsync against SQL Server: it proves network,
// credentials and database are all reachable, not just that the process is alive.
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<SanLorenzoDbContext>(
        name: "sqlserver",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["db", "ready"]);

builder.Services.AddOpenApi();

// Replaces Kestrel with the Lambda shim; a no-op outside Lambda, so the same binary runs
// locally. AddAWSLambdaBeforeSnapshotRequest replays fake requests right before the
// snapshot so the ASP.NET pipeline is already JIT-compiled inside the image. Neither route
// touches the database on purpose: TCP connections do not survive a snapshot.
builder.Services
    .AddAWSLambdaHosting(LambdaEventSource.HttpApi)
    .AddAWSLambdaBeforeSnapshotRequest(new HttpRequestMessage(HttpMethod.Get, "/health/live"))
    // Without an Authorization header this 401s, which is the point: it exercises routing,
    // authentication, authorization and the exception handler.
    .AddAWSLambdaBeforeSnapshotRequest(new HttpRequestMessage(HttpMethod.Get, "/docente/inicio"));

var app = builder.Build();

// No UseHttpsRedirection: API Gateway already terminated TLS and the shim sees plain HTTP,
// so leaving it on would 307 every request.
app.UseExceptionHandler();
app.UseCors(corsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// Liveness: does the process answer? Never touches the database, so a monitor can poll it
// without opening RDS connections.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthCheckResponse.WriteAsync,
});

// Readiness: can we also reach SQL Server? This is the one to check after a deploy or a
// network change (moving the function into the VPC).
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponse.WriteAsync,
});

app.Run();

// Required for WebApplicationFactory<Program> in the integration tests.
public partial class Program;
