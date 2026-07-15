using System.Security.Claims;
using ServerlessAPI.Entities;
using ServerlessAPI.Infrastructure;

namespace ServerlessAPI.Authentication;

/// <summary>
///     Reads the caller's identity from the JWT claims. Role and profile id travel inside the
///     signed token, so a client cannot forge them the way it could with a plain header.
/// </summary>
public sealed class UserContext(IHttpContextAccessor accessor) : IUserContext
{
    private ClaimsPrincipal Principal =>
        accessor.HttpContext?.User ?? throw new UnauthorizedException("No authenticated user.");

    public int UserId => ReadInt(ClaimTypes.NameIdentifier, "sub");

    public Role Role
    {
        get
        {
            var value = Principal.FindFirstValue(ClaimTypes.Role);

            return !Enum.TryParse<Role>(value, true, out var role)
                ? throw new UnauthorizedException("Token carries no valid role.")
                : role;
        }
    }

    public int TeacherId => ReadProfileId(AppClaims.TeacherId, Role.Teacher);

    public int StudentId => ReadProfileId(AppClaims.StudentId, Role.Student);

    /// <summary>
    ///     Reaching here with the wrong role means an endpoint is missing its
    ///     [Authorize(Roles = ...)], so fail loudly instead of returning someone else's data.
    /// </summary>
    private int ReadProfileId(string claim, Role expected)
    {
        return Role != expected
            ? throw new ForbiddenException($"Caller is {Role}; this operation requires {expected}.")
            : ReadInt(claim);
    }

    private int ReadInt(params string[] candidateClaims)
    {
        foreach (var claim in candidateClaims)
            if (int.TryParse(Principal.FindFirstValue(claim), out var value) && value > 0)
                return value;

        throw new UnauthorizedException($"Token carries no valid '{candidateClaims[0]}'.");
    }
}