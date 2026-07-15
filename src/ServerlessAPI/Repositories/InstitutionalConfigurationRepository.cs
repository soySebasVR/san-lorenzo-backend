using AWS.Lambda.Powertools.Tracing;
using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Data;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Infrastructure;

namespace ServerlessAPI.Repositories;

public sealed class InstitutionalConfigurationRepository(
    SanLorenzoDbContext db) : IInstitutionalConfigurationRepository
{
    [Tracing(SegmentName = "Get institutional configuration")]
    public async Task<InstitutionalConfigurationResponse?> GetAsync(
        CancellationToken ct = default)
    {
        return await db.InstitutionalConfigurations
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .Select(c => new InstitutionalConfigurationResponse(
                c.Id,
                c.InstitutionName,
                c.AcademicYear,
                c.AcademicPeriod,
                c.AttendanceToleranceMinutes,
                c.AbsenceAlertPercentage,
                c.TimeZone,
                c.UpdatedAt))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    [Tracing(SegmentName = "Update institutional configuration")]
    public async Task<InstitutionalConfigurationResponse> UpsertAsync(
        int userId,
        UpdateInstitutionalConfigurationRequest request,
        CancellationToken ct = default)
    {
        var coordinatorExists = await db.Users
            .AnyAsync(
                u => u.Id == userId
                     && u.Role == Role.Coordinator
                     && u.IsActive,
                ct)
            .ConfigureAwait(false);

        if (!coordinatorExists)
            throw new NotFoundException(
                $"Coordinator user {userId} does not exist or is inactive.");

        var configuration = await db.InstitutionalConfigurations
            .OrderBy(c => c.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var now = DateTime.UtcNow;

        if (configuration is null)
        {
            configuration = new InstitutionalConfiguration
            {
                InstitutionName = request.InstitutionName.Trim(),
                AcademicYear = request.AcademicYear,
                AcademicPeriod = request.AcademicPeriod.Trim(),
                AttendanceToleranceMinutes =
                    request.AttendanceToleranceMinutes,
                AbsenceAlertPercentage =
                    request.AbsenceAlertPercentage,
                TimeZone = request.TimeZone.Trim(),
                UpdatedAt = now,
                UpdatedByUserId = userId,
            };

            db.InstitutionalConfigurations.Add(configuration);
        }
        else
        {
            configuration.InstitutionName =
                request.InstitutionName.Trim();

            configuration.AcademicYear =
                request.AcademicYear;

            configuration.AcademicPeriod =
                request.AcademicPeriod.Trim();

            configuration.AttendanceToleranceMinutes =
                request.AttendanceToleranceMinutes;

            configuration.AbsenceAlertPercentage =
                request.AbsenceAlertPercentage;

            configuration.TimeZone =
                request.TimeZone.Trim();

            configuration.UpdatedAt = now;
            configuration.UpdatedByUserId = userId;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new InstitutionalConfigurationResponse(
            configuration.Id,
            configuration.InstitutionName,
            configuration.AcademicYear,
            configuration.AcademicPeriod,
            configuration.AttendanceToleranceMinutes,
            configuration.AbsenceAlertPercentage,
            configuration.TimeZone,
            configuration.UpdatedAt);
    }
}
