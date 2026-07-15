using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Infrastructure;
using ServerlessAPI.Repositories;
using Xunit;

namespace ServerlessAPI.Tests;

public sealed class CoordinatorProfileRepositoryTests : SqliteTestBase
{
    private const int CoordinatorUserId = 1;
    private const int OtherCoordinatorUserId = 2;

    protected override Task SeedAsync()
    {
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
            });

        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetAsync_returns_user_data_and_default_preferences()
    {
        var repository =
            new CoordinatorProfileRepository(Db);

        var response = await repository.GetAsync(
            CoordinatorUserId,
            Ct);

        Assert.NotNull(response);
        Assert.Equal(
            "Coordinador Principal",
            response.FullName);
        Assert.Equal(
            "coordinador@sanlorenzo.edu.pe",
            response.Email);
        Assert.Null(response.Phone);
        Assert.Null(response.ManagementArea);
        Assert.True(response.EmailNotifications);
        Assert.True(response.AppNotifications);
        Assert.Null(response.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_creates_profile_and_updates_user_data()
    {
        var repository =
            new CoordinatorProfileRepository(Db);

        var response = await repository.UpdateAsync(
            CoordinatorUserId,
            new UpdateCoordinatorProfileRequest
            {
                FullName = "Coordinador Actualizado",
                Email = "coordinador.actualizado@sanlorenzo.edu.pe",
                Phone = "999888777",
                ManagementArea = "Gestión Académica",
                EmailNotifications = false,
                AppNotifications = true,
            },
            Ct);

        Assert.Equal(
            "Coordinador Actualizado",
            response.FullName);
        Assert.Equal(
            "coordinador.actualizado@sanlorenzo.edu.pe",
            response.Email);
        Assert.Equal("999888777", response.Phone);
        Assert.Equal(
            "Gestión Académica",
            response.ManagementArea);
        Assert.False(response.EmailNotifications);
        Assert.True(response.AppNotifications);
        Assert.NotNull(response.UpdatedAt);

        var user = await Db.Users
            .AsNoTracking()
            .SingleAsync(
                u => u.Id == CoordinatorUserId,
                Ct);

        Assert.Equal(
            "Coordinador Actualizado",
            user.FullName);
        Assert.Equal(
            "coordinador.actualizado@sanlorenzo.edu.pe",
            user.Email);

        var profile = await Db.CoordinatorProfiles
            .AsNoTracking()
            .SingleAsync(
                p => p.UserId == CoordinatorUserId,
                Ct);

        Assert.Equal("999888777", profile.Phone);
        Assert.Equal(
            "Gestión Académica",
            profile.ManagementArea);
    }

    [Fact]
    public async Task UpdateAsync_rejects_an_email_used_by_another_user()
    {
        var repository =
            new CoordinatorProfileRepository(Db);

        var request = new UpdateCoordinatorProfileRequest
        {
            FullName = "Coordinador Principal",
            Email = "otro.coordinador@sanlorenzo.edu.pe",
            Phone = "999999999",
            ManagementArea = "Coordinación",
            EmailNotifications = true,
            AppNotifications = true,
        };

        await Assert.ThrowsAsync<ConflictException>(
            () => repository.UpdateAsync(
                CoordinatorUserId,
                request,
                Ct));

        Assert.Empty(
            await Db.CoordinatorProfiles
                .AsNoTracking()
                .ToListAsync(Ct));
    }
}
