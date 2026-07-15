using AWS.Lambda.Powertools.Tracing;
using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Data;
using ServerlessAPI.Dtos;
using ServerlessAPI.Infrastructure;

namespace ServerlessAPI.Repositories;

public sealed class ProfileRepository(SanLorenzoDbContext db) : IProfileRepository
{
    [Tracing(SegmentName = "Get profile")]
    public async Task<ProfileResponse?> GetProfileAsync(int teacherId, CancellationToken ct = default)
    {
        return await db.Teachers
            .AsNoTracking()
            .Where(t => t.Id == teacherId)
            .Select(t => new ProfileResponse(
                t.Id, t.FullName, t.Email, t.Position, t.Subjects,
                t.EmailNotifications, t.AppNotifications))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    [Tracing(SegmentName = "Update profile")]
    public async Task<ProfileResponse> UpdateProfileAsync(
        int teacherId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var teacher = await db.Teachers
            .FirstOrDefaultAsync(t => t.Id == teacherId, ct)
            .ConfigureAwait(false);

        if (teacher is null)
            throw new NotFoundException($"Teacher {teacherId} does not exist.");

        teacher.FullName = request.FullName;
        teacher.Email = request.Email;
        teacher.Subjects = request.Subjects;
        teacher.EmailNotifications = request.EmailNotifications;
        teacher.AppNotifications = request.AppNotifications;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Position is not editable from the profile.
        return new ProfileResponse(
            teacher.Id, teacher.FullName, teacher.Email, teacher.Position,
            teacher.Subjects, teacher.EmailNotifications, teacher.AppNotifications);
    }
}
