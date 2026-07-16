using System.ComponentModel.DataAnnotations;

namespace ServerlessAPI.Dtos;

public record CreateBroadcastRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Subject { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public string Body { get; init; } = string.Empty;

    /// <summary>"Students", "Teachers", "Parents" o "All".</summary>
    [Required]
    public string Audience { get; init; } = string.Empty;

    [StringLength(20)]
    public string? GradeLevel { get; init; }

    [Required]
    public DateTime ScheduledFor { get; init; }
}

public record BroadcastResponse(
    int Id,
    string Subject,
    string Body,
    string Audience,
    string? GradeLevel,
    DateTime ScheduledFor,
    DateTime CreatedAt);
