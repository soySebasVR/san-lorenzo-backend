using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using ServerlessAPI.Entities;

namespace ServerlessAPI.Authentication;

public sealed class TokenService(JwtKeyProvider keys)
{
    public (string Token, DateTime ExpiresAt) Issue(User user)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.Add(JwtKeyProvider.Lifetime);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, user.Role.ToString()),
        };

        // El id del perfil va en el token para evitar consultas a la DB.
        if (user.TeacherId is { } teacherId)
            claims.Add(new Claim(AppClaims.TeacherId, teacherId.ToString()));

        if (user.StudentId is { } studentId)
            claims.Add(new Claim(AppClaims.StudentId, studentId.ToString()));

        var token = new JwtSecurityToken(
            issuer: keys.Issuer,
            audience: keys.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(keys.Key, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
