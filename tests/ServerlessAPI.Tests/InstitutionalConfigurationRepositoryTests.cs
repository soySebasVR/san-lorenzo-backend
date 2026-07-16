using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Infrastructure;
using ServerlessAPI.Repositories;
using Xunit;

namespace ServerlessAPI.Tests;

public sealed class InstitutionalConfigurationRepositoryTests : SqliteTestBase
{
    private const int CoordinatorUserId = 1;
    private const int InactiveCoordinatorUserId = 2;

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
                Id = InactiveCoordinatorUserId,
                Email = "coordinador.inactivo@sanlorenzo.edu.pe",
                FullName = "Coordinador Inactivo",
                PasswordHash = "hash-prueba",
                Role = Role.Coordinator,
                IsActive = false,
            });

        return Task.CompletedTask;
    }

    private static UpdateInstitutionalConfigurationRequest CreateRequest(
        string institutionName = "Institución Educativa San Lorenzo",
        int academicYear = 2026)
    {
        return new UpdateInstitutionalConfigurationRequest
        {
            InstitutionName = institutionName,
            AcademicYear = academicYear,
            AcademicPeriod = "Año lectivo 2026",
            AttendanceToleranceMinutes = 10,
            AbsenceAlertPercentage = 30,
            TimeZone = "America/Lima",
        };
    }

    [Fact]
    public async Task UpsertAsync_creates_the_initial_configuration()
    {
        var repository =
            new InstitutionalConfigurationRepository(Db);

        var response = await repository.UpsertAsync(
            CoordinatorUserId,
            CreateRequest(),
            Ct);

        Assert.True(response.Id > 0);
        Assert.Equal(
            "Institución Educativa San Lorenzo",
            response.InstitutionName);
        Assert.Equal(2026, response.AcademicYear);
        Assert.Equal("America/Lima", response.TimeZone);

        var saved = await Db.InstitutionalConfigurations
            .AsNoTracking()
            .SingleAsync(Ct);

        Assert.Equal(CoordinatorUserId, saved.UpdatedByUserId);
        Assert.Equal(10, saved.AttendanceToleranceMinutes);
        Assert.Equal(30m, saved.AbsenceAlertPercentage);
    }

    [Fact]
    public async Task UpsertAsync_updates_the_existing_configuration_without_duplicates()
    {
        var repository =
            new InstitutionalConfigurationRepository(Db);

        var created = await repository.UpsertAsync(
            CoordinatorUserId,
            CreateRequest(),
            Ct);

        var updated = await repository.UpsertAsync(
            CoordinatorUserId,
            CreateRequest(
                institutionName: "San Lorenzo Actualizado",
                academicYear: 2027),
            Ct);

        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("San Lorenzo Actualizado", updated.InstitutionName);
        Assert.Equal(2027, updated.AcademicYear);

        Assert.Equal(
            1,
            await Db.InstitutionalConfigurations.CountAsync(Ct));
    }

    [Fact]
    public async Task UpsertAsync_rejects_an_inactive_coordinator()
    {
        var repository =
            new InstitutionalConfigurationRepository(Db);

        await Assert.ThrowsAsync<NotFoundException>(
            () => repository.UpsertAsync(
                InactiveCoordinatorUserId,
                CreateRequest(),
                Ct));

        Assert.Empty(
            await Db.InstitutionalConfigurations
                .AsNoTracking()
                .ToListAsync(Ct));
    }
}
