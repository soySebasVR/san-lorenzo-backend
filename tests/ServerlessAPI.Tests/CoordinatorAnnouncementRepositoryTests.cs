using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Infrastructure;
using ServerlessAPI.Repositories;
using Xunit;

namespace ServerlessAPI.Tests;

public sealed class CoordinatorAnnouncementRepositoryTests : SqliteTestBase
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
                FullName = "Otro Coordinador",
                PasswordHash = "hash-prueba",
                Role = Role.Coordinator,
                IsActive = true,
            });

        return Task.CompletedTask;
    }

    private static CoordinatorAnnouncementRecipientRequest AllRecipient() =>
        new()
        {
            TargetType = "All",
        };

    private static CoordinatorAnnouncementRecipientRequest RoleRecipient(
        string role = "Teacher") =>
        new()
        {
            TargetType = "Role",
            TargetRole = role,
        };

    private static CoordinatorAnnouncementRecipientRequest GradeRecipient() =>
        new()
        {
            TargetType = "GradeSection",
            GradeLevel = "5to",
            Section = "A",
        };

    [Fact]
    public async Task CreateAsync_creates_a_draft_with_its_recipients()
    {
        var repository = new CoordinatorAnnouncementRepository(Db);

        var response = await repository.CreateAsync(
            CoordinatorUserId,
            new CreateCoordinatorAnnouncementRequest
            {
                Title = "Reunión institucional",
                Content = "Se comunica la realización de la reunión.",
                Status = "Draft",
                Recipients = [AllRecipient()],
            },
            Ct);

        Assert.True(response.Id > 0);
        Assert.Equal("Draft", response.Status);
        Assert.Null(response.SentAt);

        var recipient = Assert.Single(response.Recipients);
        Assert.Equal("All", recipient.TargetType);

        var saved = await Db.CoordinatorAnnouncements
            .AsNoTracking()
            .Include(a => a.Recipients)
            .SingleAsync(Ct);

        Assert.Equal(CoordinatorUserId, saved.CreatedByUserId);
        Assert.Equal("Reunión institucional", saved.Title);
        Assert.Single(saved.Recipients);
    }

    [Fact]
    public async Task CreateAsync_rejects_a_scheduled_announcement_in_the_past()
    {
        var repository = new CoordinatorAnnouncementRepository(Db);

        var request = new CreateCoordinatorAnnouncementRequest
        {
            Title = "Comunicado programado",
            Content = "Contenido del comunicado.",
            Status = "Scheduled",
            ScheduledAt = DateTime.UtcNow.AddMinutes(-10),
            Recipients = [RoleRecipient()],
        };

        await Assert.ThrowsAsync<BadRequestException>(
            () => repository.CreateAsync(
                CoordinatorUserId,
                request,
                Ct));

        Assert.Empty(
            await Db.CoordinatorAnnouncements
                .AsNoTracking()
                .ToListAsync(Ct));
    }

    [Fact]
    public async Task CreateAsync_rejects_all_combined_with_other_recipients()
    {
        var repository = new CoordinatorAnnouncementRepository(Db);

        var request = new CreateCoordinatorAnnouncementRequest
        {
            Title = "Comunicado general",
            Content = "Contenido del comunicado.",
            Status = "Draft",
            Recipients =
            [
                AllRecipient(),
                RoleRecipient("Student"),
            ],
        };

        await Assert.ThrowsAsync<BadRequestException>(
            () => repository.CreateAsync(
                CoordinatorUserId,
                request,
                Ct));
    }

    [Fact]
    public async Task GetAsync_filters_by_creator_and_status()
    {
        var repository = new CoordinatorAnnouncementRepository(Db);

        await repository.CreateAsync(
            CoordinatorUserId,
            new CreateCoordinatorAnnouncementRequest
            {
                Title = "Borrador",
                Content = "Contenido del borrador.",
                Status = "Draft",
                Recipients = [AllRecipient()],
            },
            Ct);

        await repository.CreateAsync(
            CoordinatorUserId,
            new CreateCoordinatorAnnouncementRequest
            {
                Title = "Comunicado enviado",
                Content = "Contenido enviado.",
                Status = "Sent",
                Recipients = [RoleRecipient("Teacher")],
            },
            Ct);

        await repository.CreateAsync(
            OtherCoordinatorUserId,
            new CreateCoordinatorAnnouncementRequest
            {
                Title = "Comunicado de otro coordinador",
                Content = "Contenido que no debe aparecer.",
                Status = "Sent",
                Recipients = [AllRecipient()],
            },
            Ct);

        var result = await repository.GetAsync(
            CoordinatorUserId,
            "Sent",
            Ct);

        var announcement = Assert.Single(result);

        Assert.Equal("Comunicado enviado", announcement.Title);
        Assert.Equal("Sent", announcement.Status);
        Assert.NotNull(announcement.SentAt);
    }

    [Fact]
    public async Task UpdateAsync_replaces_recipients_and_schedules_the_announcement()
    {
        var repository = new CoordinatorAnnouncementRepository(Db);

        var created = await repository.CreateAsync(
            CoordinatorUserId,
            new CreateCoordinatorAnnouncementRequest
            {
                Title = "Comunicado inicial",
                Content = "Contenido inicial.",
                Status = "Draft",
                Recipients = [RoleRecipient("Teacher")],
            },
            Ct);

        Db.ChangeTracker.Clear();

        var futureDate = DateTime.UtcNow.AddDays(1);

        var updated = await repository.UpdateAsync(
            CoordinatorUserId,
            created.Id,
            new UpdateCoordinatorAnnouncementRequest
            {
                Title = "Comunicado actualizado",
                Content = "Contenido actualizado.",
                Status = "Scheduled",
                ScheduledAt = futureDate,
                Recipients = [GradeRecipient()],
            },
            Ct);

        Assert.Equal("Comunicado actualizado", updated.Title);
        Assert.Equal("Scheduled", updated.Status);
        Assert.NotNull(updated.ScheduledAt);

        var recipient = Assert.Single(updated.Recipients);

        Assert.Equal("GradeSection", recipient.TargetType);
        Assert.Equal("5to", recipient.GradeLevel);
        Assert.Equal("A", recipient.Section);

        var savedRecipients = await Db.CoordinatorAnnouncementRecipients
            .AsNoTracking()
            .Where(r => r.CoordinatorAnnouncementId == created.Id)
            .ToListAsync(Ct);

        Assert.Single(savedRecipients);
        Assert.Equal(
            CoordinatorRecipientType.GradeSection,
            savedRecipients[0].TargetType);
    }

    [Fact]
    public async Task UpdateAsync_rejects_modifying_a_sent_announcement()
    {
        var repository = new CoordinatorAnnouncementRepository(Db);

        var created = await repository.CreateAsync(
            CoordinatorUserId,
            new CreateCoordinatorAnnouncementRequest
            {
                Title = "Comunicado enviado",
                Content = "Contenido enviado.",
                Status = "Sent",
                Recipients = [AllRecipient()],
            },
            Ct);

        Db.ChangeTracker.Clear();

        var request = new UpdateCoordinatorAnnouncementRequest
        {
            Title = "Intento de modificación",
            Content = "Este cambio no debe guardarse.",
            Status = "Draft",
            Recipients = [AllRecipient()],
        };

        await Assert.ThrowsAsync<BadRequestException>(
            () => repository.UpdateAsync(
                CoordinatorUserId,
                created.Id,
                request,
                Ct));
    }
}
