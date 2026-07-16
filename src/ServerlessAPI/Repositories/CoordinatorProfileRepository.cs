using AWS.Lambda.Powertools.Tracing;
using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Data;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Infrastructure;

namespace ServerlessAPI.Repositories;

/// <summary>
/// The coordinator has no academic profile, so their profile lives on the User row.
/// </summary>
public sealed class CoordinatorProfileRepository(SanLorenzoDbContext db) : ICoordinatorProfileRepository
{
    [Tracing(SegmentName = "Get coordinator profile")]
    public async Task<CoordinatorProfileResponse?> GetAsync(int userId, CancellationToken ct = default) =>
        await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new CoordinatorProfileResponse(
                u.Id, u.FullName, u.Email, u.EmailNotifications, u.AppNotifications))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

    [Tracing(SegmentName = "Update coordinator profile")]
    public async Task<CoordinatorProfileResponse> UpdateAsync(
        int userId, UpdateCoordinatorProfileRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct).ConfigureAwait(false)
                   ?? throw new NotFoundException($"User {userId} does not exist.");

        var emailTaken = await db.Users.AsNoTracking()
            .AnyAsync(u => u.Email == request.Email && u.Id != userId, ct)
            .ConfigureAwait(false);
        if (emailTaken)
            throw new ForbiddenException($"Email '{request.Email}' is already registered.");

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.EmailNotifications = request.EmailNotifications;
        user.AppNotifications = request.AppNotifications;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new CoordinatorProfileResponse(
            user.Id, user.FullName, user.Email, user.EmailNotifications, user.AppNotifications);
    }
}
