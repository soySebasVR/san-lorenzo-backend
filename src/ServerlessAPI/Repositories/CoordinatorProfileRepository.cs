using AWS.Lambda.Powertools.Tracing;
using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Data;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Infrastructure;

namespace ServerlessAPI.Repositories;

public sealed class CoordinatorProfileRepository(
    SanLorenzoDbContext db) : ICoordinatorProfileRepository
{
    [Tracing(SegmentName = "Get coordinator profile")]
    public async Task<CoordinatorProfileResponse?> GetAsync(
        int userId,
        CancellationToken ct = default)
    {
        var user = await db.Users
            .AsNoTracking()
            .Where(u =>
                u.Id == userId
                && u.Role == Role.Coordinator
                && u.IsActive)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (user is null)
            return null;

        var profile = await db.CoordinatorProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct)
            .ConfigureAwait(false);

        return new CoordinatorProfileResponse(
            user.Id,
            user.FullName,
            user.Email,
            profile?.Phone,
            profile?.ManagementArea,
            profile?.EmailNotifications ?? true,
            profile?.AppNotifications ?? true,
            profile?.UpdatedAt);
    }

    [Tracing(SegmentName = "Update coordinator profile")]
    public async Task<CoordinatorProfileResponse> UpdateAsync(
        int userId,
        UpdateCoordinatorProfileRequest request,
        CancellationToken ct = default)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(
                u => u.Id == userId
                     && u.Role == Role.Coordinator
                     && u.IsActive,
                ct)
            .ConfigureAwait(false);

        if (user is null)
            throw new NotFoundException(
                $"Coordinator user {userId} does not exist or is inactive.");

        var email = request.Email.Trim();

        var emailInUse = await db.Users
            .AnyAsync(
                u => u.Id != userId && u.Email == email,
                ct)
            .ConfigureAwait(false);

        if (emailInUse)
            throw new ConflictException(
                $"The email '{email}' is already assigned to another user.");

        var profile = await db.CoordinatorProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, ct)
            .ConfigureAwait(false);

        var now = DateTime.UtcNow;

        if (profile is null)
        {
            profile = new CoordinatorProfile
            {
                UserId = userId,
                Phone = NormalizeOptional(request.Phone),
                ManagementArea =
                    NormalizeOptional(request.ManagementArea),
                EmailNotifications =
                    request.EmailNotifications,
                AppNotifications =
                    request.AppNotifications,
                UpdatedAt = now,
            };

            db.CoordinatorProfiles.Add(profile);
        }
        else
        {
            profile.Phone =
                NormalizeOptional(request.Phone);

            profile.ManagementArea =
                NormalizeOptional(request.ManagementArea);

            profile.EmailNotifications =
                request.EmailNotifications;

            profile.AppNotifications =
                request.AppNotifications;

            profile.UpdatedAt = now;
        }

        user.FullName = request.FullName.Trim();
        user.Email = email;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new CoordinatorProfileResponse(
            user.Id,
            user.FullName,
            user.Email,
            profile.Phone,
            profile.ManagementArea,
            profile.EmailNotifications,
            profile.AppNotifications,
            profile.UpdatedAt);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
