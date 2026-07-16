using AWS.Lambda.Powertools.Tracing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Data;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;
using ServerlessAPI.Infrastructure;

namespace ServerlessAPI.Repositories;

public sealed class UserAdminRepository(
    SanLorenzoDbContext db,
    IPasswordHasher<User> hasher) : IUserAdminRepository
{
    [Tracing(SegmentName = "List users")]
    public async Task<IReadOnlyList<UserListItem>> ListAsync(string? search, CancellationToken ct = default)
    {
        var query = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u => u.FullName.Contains(term) || u.Email.Contains(term));
        }

        return await query
            .OrderBy(u => u.FullName)
            .Select(u => new UserListItem(u.Id, u.FullName, u.Email, u.Role.ToString(), u.IsActive))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    [Tracing(SegmentName = "Get user")]
    public async Task<UserDetail?> GetAsync(int userId, CancellationToken ct = default) =>
        await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserDetail(
                u.Id, u.FullName, u.Email, u.Role.ToString(), u.IsActive, u.TeacherId, u.StudentId))
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

    [Tracing(SegmentName = "Create user")]
    public async Task<UserDetail> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<Role>(request.Role, ignoreCase: true, out var role))
            throw new ForbiddenException($"Unknown role '{request.Role}'.");

        var emailTaken = await db.Users.AsNoTracking()
            .AnyAsync(u => u.Email == request.Email, ct).ConfigureAwait(false);
        if (emailTaken)
            throw new ForbiddenException($"Email '{request.Email}' is already registered.");

        // Enforce the same profile↔role rule the DB check constraint does, with clear errors.
        await ValidateProfileAsync(role, request.TeacherId, request.StudentId, ct);

        var user = new User
        {
            Email = request.Email,
            FullName = request.FullName,
            Role = role,
            IsActive = true,
            TeacherId = role == Role.Teacher ? request.TeacherId : null,
            StudentId = role == Role.Student ? request.StudentId : null,
        };
        user.PasswordHash = hasher.HashPassword(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new UserDetail(
            user.Id, user.FullName, user.Email, user.Role.ToString(), user.IsActive,
            user.TeacherId, user.StudentId);
    }

    [Tracing(SegmentName = "Update user")]
    public async Task<UserDetail?> UpdateAsync(
        int userId, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct).ConfigureAwait(false);
        if (user is null)
            return null;

        user.FullName = request.FullName;
        user.IsActive = request.IsActive;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new UserDetail(
            user.Id, user.FullName, user.Email, user.Role.ToString(), user.IsActive,
            user.TeacherId, user.StudentId);
    }

    private async Task ValidateProfileAsync(Role role, int? teacherId, int? studentId, CancellationToken ct)
    {
        switch (role)
        {
            case Role.Teacher:
                if (teacherId is null || !await db.Teachers.AnyAsync(t => t.Id == teacherId, ct))
                    throw new ForbiddenException("A teacher account needs an existing teacherId.");
                break;

            case Role.Student:
                if (studentId is null || !await db.Students.AnyAsync(s => s.Id == studentId, ct))
                    throw new ForbiddenException("A student account needs an existing studentId.");
                break;

            case Role.Coordinator:
                if (teacherId is not null || studentId is not null)
                    throw new ForbiddenException("A coordinator account must not reference a profile.");
                break;
        }
    }
}
