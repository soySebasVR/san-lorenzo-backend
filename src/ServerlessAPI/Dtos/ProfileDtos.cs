using System.ComponentModel.DataAnnotations;

namespace ServerlessAPI.Dtos;

public record ProfileResponse(
    int TeacherId,
    string FullName,
    string Email,
    string Position,
    string? Subjects,
    bool EmailNotifications,
    bool AppNotifications);

public record UpdateProfileRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; init; } = string.Empty;

    [StringLength(255)]
    public string? Subjects { get; init; }

    public bool EmailNotifications { get; init; } = true;
    public bool AppNotifications { get; init; }
}
