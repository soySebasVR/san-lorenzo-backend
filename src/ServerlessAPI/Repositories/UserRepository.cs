using AWS.Lambda.Powertools.Tracing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Data;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;

namespace ServerlessAPI.Repositories;

public sealed class UserRepository(
    SanLorenzoDbContext db,
    IPasswordHasher<User> hasher,
    ILogger<UserRepository> logger) : IUserRepository
{
    /// <summary>
    /// Verified against when the email does not exist, so a missing account takes the same
    /// time as a wrong password. Otherwise timing alone reveals which emails are registered.
    /// </summary>
    private static readonly string DecoyHash =
        new PasswordHasher<User>().HashPassword(new User(), "no-such-password");

    [Tracing(SegmentName = "Authenticate user")]
    public async Task<User?> AuthenticateAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await db.Users
            .Include(u => u.Teacher)
            .Include(u => u.Student)
            .FirstOrDefaultAsync(u => u.Email == email, ct)
            .ConfigureAwait(false);

        if (user is null)
        {
            hasher.VerifyHashedPassword(new User(), DecoyHash, password);
            logger.LogWarning("Login failed: no account for {Email}", email);
            return null;
        }

        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, password);

        if (result == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("Login failed: wrong password for {Email}", email);
            return null;
        }

        // Checked after the password so an attacker cannot learn that the account exists.
        if (!user.IsActive)
        {
            logger.LogWarning("Login failed: account {Email} is deactivated", email);
            return null;
        }

        // The stored hash used weaker parameters than the current hasher. We hold the
        // plaintext right now, so this is the only moment we can upgrade it.
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = hasher.HashPassword(user, password);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            logger.LogInformation("Password hash upgraded for {Email}", email);
        }

        return user;
    }

    [Tracing(SegmentName = "Get current user")]
    public async Task<CurrentUser?> GetCurrentAsync(int userId, CancellationToken ct = default)
    {
        return await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && u.IsActive)
            .Select(u => new CurrentUser(
                u.Id, u.Email, u.Role.ToString(), u.FullName, u.TeacherId, u.StudentId))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }
}
