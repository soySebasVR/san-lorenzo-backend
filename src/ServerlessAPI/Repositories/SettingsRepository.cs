using AWS.Lambda.Powertools.Tracing;
using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Data;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;

namespace ServerlessAPI.Repositories;

public sealed class SettingsRepository(SanLorenzoDbContext db) : ISettingsRepository
{
    // The single settings row.
    private const int SettingsId = 1;

    [Tracing(SegmentName = "Get settings")]
    public async Task<SettingsResponse> GetAsync(CancellationToken ct = default)
    {
        var settings = await LoadAsync(ct);
        return ToResponse(settings);
    }

    [Tracing(SegmentName = "Update settings")]
    public async Task<SettingsResponse> UpdateAsync(UpdateSettingsRequest request, CancellationToken ct = default)
    {
        var settings = await LoadAsync(ct);

        settings.SchoolName = request.SchoolName;
        settings.AcademicYear = request.AcademicYear;
        settings.CurrentTerm = request.CurrentTerm;
        settings.UnjustifiedAbsenceThreshold = request.UnjustifiedAbsenceThreshold;
        settings.LatenessToleranceMinutes = request.LatenessToleranceMinutes;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return ToResponse(settings);
    }

    /// <summary>Loads the settings row, creating a default one on first use.</summary>
    private async Task<SystemSettings> LoadAsync(CancellationToken ct)
    {
        var settings = await db.SystemSettings
            .FirstOrDefaultAsync(s => s.Id == SettingsId, ct)
            .ConfigureAwait(false);

        if (settings is null)
        {
            settings = new SystemSettings
            {
                Id = SettingsId,
                SchoolName = "Colegio San Lorenzo",
                AcademicYear = DateTime.UtcNow.Year,
                CurrentTerm = $"{DateTime.UtcNow.Year}-I",
                UnjustifiedAbsenceThreshold = 3,
                LatenessToleranceMinutes = 10,
            };
            db.SystemSettings.Add(settings);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return settings;
    }

    private static SettingsResponse ToResponse(SystemSettings s) =>
        new(s.SchoolName, s.AcademicYear, s.CurrentTerm, s.UnjustifiedAbsenceThreshold, s.LatenessToleranceMinutes);
}
