using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ServerlessAPI.Entities;
using ServerlessAPI.Repositories;
using Xunit;

namespace ServerlessAPI.Tests;

public sealed class UserRepositoryTests : SqliteTestBase
{
    private const string CorrectPassword = "Docente#2026";

    private static readonly PasswordHasher<User> Hasher = new();

    protected override Task SeedAsync()
    {
        Db.Teachers.Add(new Teacher
        {
            Id = 1, FullName = "Ana Torres", Email = "ana@sl.edu", Position = "Docente",
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
        teacher.PasswordHash = Hasher.HashPassword(teacher, CorrectPassword);

        var deactivated = new User
        {
            Id = 2,
            Email = "inactive@sl.edu",
            FullName = "Deactivated Account",
            Role = Role.Coordinator,
            IsActive = false,
        };
        deactivated.PasswordHash = Hasher.HashPassword(deactivated, CorrectPassword);

        Db.Users.AddRange(teacher, deactivated);

        return Task.CompletedTask;
    }

    private UserRepository CreateRepo() =>
        new(Db, Hasher, NullLogger<UserRepository>.Instance);

    [Fact]
    public async Task Authenticate_with_valid_credentials_returns_the_user()
    {
        var user = await CreateRepo().AuthenticateAsync("teacher@sl.edu", CorrectPassword, Ct);

        Assert.NotNull(user);
        Assert.Equal(Role.Teacher, user.Role);
        Assert.Equal(1, user.TeacherId);
    }

    [Fact]
    public async Task Authenticate_with_wrong_password_returns_null()
    {
        var user = await CreateRepo().AuthenticateAsync("teacher@sl.edu", "something-else", Ct);

        Assert.Null(user);
    }

    [Fact]
    public async Task Authenticate_with_unknown_email_returns_null()
    {
        var user = await CreateRepo().AuthenticateAsync("nobody@sl.edu", CorrectPassword, Ct);

        Assert.Null(user);
    }

    /// <summary>Deactivating is how someone is removed without deleting their history.</summary>
    [Fact]
    public async Task Authenticate_a_deactivated_account_returns_null()
    {
        var user = await CreateRepo().AuthenticateAsync("inactive@sl.edu", CorrectPassword, Ct);

        Assert.Null(user);
    }

    [Fact]
    public async Task The_password_is_never_stored_in_plain_text()
    {
        var stored = await Db.Users
            .AsNoTracking()
            .Where(u => u.Email == "teacher@sl.edu")
            .Select(u => u.PasswordHash)
            .SingleAsync(Ct);

        Assert.DoesNotContain(CorrectPassword, stored, StringComparison.OrdinalIgnoreCase);
        // PasswordHasher v3 format starts with 0x01 → "AQAAAA..." in base64.
        Assert.StartsWith("AQAAAA", stored, StringComparison.Ordinal);
    }
}
