using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerlessAPI.Authentication;
using ServerlessAPI.Dtos;
using ServerlessAPI.Repositories;

namespace ServerlessAPI.Controllers;

[ApiController]
[ApiExplorerSettings(GroupName = "Auth")]
[Route("auth")]
[Produces("application/json")]
public class AuthController(
    ILogger<AuthController> logger,
    IUserRepository users,
    TokenService tokens,
    IUserContext userContext) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        var user = await users.AuthenticateAsync(request.Email, request.Password, ct);

        if (user is null)
        {
            // One message for unknown email, wrong password and deactivated account.
            // Telling them apart would reveal which emails are registered.
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Invalid credentials",
                Detail = "El correo o la contraseña no son correctos.",
            });
        }

        var (token, expiresAt) = tokens.Issue(user);

        logger.LogInformation("Login succeeded. User {UserId}, role {Role}", user.Id, user.Role);

        return Ok(new LoginResponse(
            token,
            expiresAt,
            new CurrentUser(
                user.Id,
                user.Email,
                user.Role.ToString(),
                user.FullName,
                user.TeacherId,
                user.StudentId)));
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<CurrentUser>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUser>> Me(CancellationToken ct)
    {
        var current = await users.GetCurrentAsync(userContext.UserId, ct);

        // Token is valid but the account was deleted or deactivated after it was issued.
        // With no revocation list, this is where that gets caught.
        return current is null ? Unauthorized() : Ok(current);
    }
}
