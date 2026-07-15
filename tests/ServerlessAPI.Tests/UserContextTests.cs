using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ServerlessAPI.Authentication;
using ServerlessAPI.Entities;
using ServerlessAPI.Infrastructure;
using Xunit;

namespace ServerlessAPI.Tests;

public sealed class UserContextTests
{
    private static IUserContext WithClaims(params Claim[] claims)
    {
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test")),
        };

        return new UserContext(new HttpContextAccessor { HttpContext = http });
    }

    [Fact]
    public void A_teacher_exposes_their_TeacherId()
    {
        var ctx = WithClaims(
            new Claim(ClaimTypes.NameIdentifier, "7"),
            new Claim(ClaimTypes.Role, nameof(Role.Teacher)),
            new Claim(AppClaims.TeacherId, "42"));

        Assert.Equal(7, ctx.UserId);
        Assert.Equal(Role.Teacher, ctx.Role);
        Assert.Equal(42, ctx.TeacherId);
    }

    /// <summary>
    /// Even if an endpoint were missing its [Authorize(Roles = ...)], the context refuses
    /// to hand back some arbitrary teacher's id.
    /// </summary>
    [Fact]
    public void A_coordinator_cannot_ask_for_a_TeacherId()
    {
        var ctx = WithClaims(
            new Claim(ClaimTypes.NameIdentifier, "9"),
            new Claim(ClaimTypes.Role, nameof(Role.Coordinator)));

        Assert.Throws<ForbiddenException>(() => ctx.TeacherId);
    }

    [Fact]
    public void A_student_cannot_ask_for_a_TeacherId()
    {
        var ctx = WithClaims(
            new Claim(ClaimTypes.NameIdentifier, "3"),
            new Claim(ClaimTypes.Role, nameof(Role.Student)),
            new Claim(AppClaims.StudentId, "15"));

        Assert.Equal(15, ctx.StudentId);
        Assert.Throws<ForbiddenException>(() => ctx.TeacherId);
    }

    [Fact]
    public void A_token_without_a_role_is_rejected()
    {
        var ctx = WithClaims(new Claim(ClaimTypes.NameIdentifier, "1"));

        Assert.Throws<UnauthorizedException>(() => ctx.Role);
    }

    [Fact]
    public void An_unauthenticated_request_has_no_identity()
    {
        var ctx = new UserContext(new HttpContextAccessor { HttpContext = null });

        Assert.Throws<UnauthorizedException>(() => ctx.UserId);
    }
}
