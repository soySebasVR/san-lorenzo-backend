using System.ComponentModel.DataAnnotations;

namespace ServerlessAPI.Dtos;

public record LoginRequest
{
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;
}

public record LoginResponse(
    string Token,
    DateTime ExpiresAt,
    CurrentUser User);

/// <summary>Lets the frontend pick the right layout without decoding the JWT.</summary>
public record CurrentUser(
    int Id,
    string Email,
    string Role,
    string FullName,
    int? TeacherId,
    int? StudentId);
