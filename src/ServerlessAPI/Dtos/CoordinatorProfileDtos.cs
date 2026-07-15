using System.ComponentModel.DataAnnotations;

namespace ServerlessAPI.Dtos;

public record CoordinatorProfileResponse(
    int UserId,
    string FullName,
    string Email,
    string? Phone,
    string? ManagementArea,
    bool EmailNotifications,
    bool AppNotifications,
    DateTime? UpdatedAt);

public record UpdateCoordinatorProfileRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; init; } = string.Empty;

    [Phone]
    [StringLength(20)]
    public string? Phone { get; init; }

    [StringLength(100)]
    public string? ManagementArea { get; init; }

    public bool EmailNotifications { get; init; } = true;

    public bool AppNotifications { get; init; } = true;
}
