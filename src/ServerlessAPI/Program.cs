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
using Microsoft.OpenApi;
using ServerlessAPI.Authentication;
using ServerlessAPI.Data;
using ServerlessAPI.Entities;
using ServerlessAPI.Infrastructure;
using ServerlessAPI.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Configuración de AWS Powertools Logger.
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
        // Angular usa camelCase.
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

// Precarga secretos
foreach (var secret in secrets)
    await secret.WarmAsync();

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
            ClockSkew = TimeSpan.Zero,

            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (_, _, _, _) => [jwtKeys.Key],

            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

// Hashing de contraseñas de Identity (PBKDF2).
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<TokenService>();

// ── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<SanLorenzoDbContext>((sp, options) =>
{
    var connectionString = sp.GetRequiredService<ConnectionStringProvider>().Value;

    options.UseSqlServer(connectionString, sql =>
    {
        // RDS drops connections on failover and maintenance; retrying keeps that from
        // surfacing as a 500.
        // 258 / -2 are SQL Server timeout errors that hit the first connection from a cold
        // Lambda container; adding them here makes EF retry instead of surfacing a 500.
        sql.EnableRetryOnFailure(
            5,
            TimeSpan.FromSeconds(6),
            [258, -2]);

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

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<SanLorenzoDbContext>(
        "sqlserver",
        HealthStatus.Unhealthy,
        ["db", "ready"]);

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("Auth", new OpenApiInfo { Title = "Auth API", Version = "v1" });
    c.SwaggerDoc("Coordinator", new OpenApiInfo { Title = "Coordinator API", Version = "v1" });
    c.SwaggerDoc("Student", new OpenApiInfo { Title = "Student API", Version = "v1" });
    c.SwaggerDoc("Teacher", new OpenApiInfo { Title = "Teacher API", Version = "v1" });
});

builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

var app = builder.Build();

// Sin redirección HTTPS: API Gateway.
app.UseExceptionHandler();
app.UseCors(corsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// if (app.Environment.IsDevelopment())
// {
app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/prod/swagger/Auth/swagger.json", "Auth");
    c.SwaggerEndpoint("/prod/swagger/Coordinator/swagger.json", "Coordinator");
    c.SwaggerEndpoint("/prod/swagger/Student/swagger.json", "Student");
    c.SwaggerEndpoint("/prod/swagger/Teacher/swagger.json", "Teacher");
});
// }

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthCheckResponse.WriteAsync
});

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponse.WriteAsync
});

app.Run();

// Necesario para pruebas de integración.
public partial class Program;