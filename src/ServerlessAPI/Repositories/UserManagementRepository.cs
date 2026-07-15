using Microsoft.EntityFrameworkCore;
using ServerlessAPI.Data;
using ServerlessAPI.Dtos;
using ServerlessAPI.Entities;

namespace ServerlessAPI.Repositories;

public sealed class UserManagementRepository(SanLorenzoDbContext db) : IUserManagementRepository
{
    public async Task<IReadOnlyList<UserResponse>> GetUsersAsync(CancellationToken ct = default)
    {
        return await db.Users
            .Select(u => new UserResponse(
                u.Id,
                u.Email,
                u.Email, 
                u.Role.ToString(),
                u.IsActive))
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<UserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var user = new User
        {
            Email = request.Email,
            PasswordHash = request.Password,
            IsActive = true,
            Role = Enum.Parse<Role>(request.Role, true)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return new UserResponse(user.Id, user.Email, request.FullName, request.Role, user.IsActive);
    }

    public async Task<UserResponse> UpdateUserAsync(int id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        
        if (user == null) 
        {
            throw new Exception("User not found.");
        }

        user.Email = request.Email;
        user.IsActive = request.IsActive;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = request.Password;
        }

        await db.SaveChangesAsync(ct);

        return new UserResponse(user.Id, user.Email, request.FullName, "Updated", user.IsActive);
    }
}