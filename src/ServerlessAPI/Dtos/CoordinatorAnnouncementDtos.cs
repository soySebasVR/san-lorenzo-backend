using System.ComponentModel.DataAnnotations;

namespace ServerlessAPI.Dtos;

public record CoordinatorAnnouncementRecipientRequest
{
    [Required]
    [RegularExpression(
        "^(All|Role|GradeSection)$",
        ErrorMessage = "TargetType must be All, Role or GradeSection.")]
    public string TargetType { get; init; } = string.Empty;

    [RegularExpression(
        "^(Student|Teacher|Coordinator)$",
        ErrorMessage = "TargetRole must be Student, Teacher or Coordinator.")]
    public string? TargetRole { get; init; }

    [StringLength(20)]
    public string? GradeLevel { get; init; }

    [StringLength(20)]
    public string? Section { get; init; }
}

public record CreateCoordinatorAnnouncementRequest
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Title { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string Content { get; init; } = string.Empty;

    public DateTime? ScheduledAt { get; init; }

    [Required]
    [RegularExpression(
        "^(Draft|Scheduled|Sent)$",
        ErrorMessage = "Status must be Draft, Scheduled or Sent.")]
    public string Status { get; init; } = "Draft";

    [Required]
    [MinLength(1)]
    public List<CoordinatorAnnouncementRecipientRequest> Recipients { get; init; }
        = [];
}

public record UpdateCoordinatorAnnouncementRequest
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Title { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string Content { get; init; } = string.Empty;

    public DateTime? ScheduledAt { get; init; }

    [Required]
    [RegularExpression(
        "^(Draft|Scheduled|Sent|Cancelled)$",
        ErrorMessage = "Status must be Draft, Scheduled, Sent or Cancelled.")]
    public string Status { get; init; } = "Draft";

    [Required]
    [MinLength(1)]
    public List<CoordinatorAnnouncementRecipientRequest> Recipients { get; init; }
        = [];
}

public record CoordinatorAnnouncementRecipientResponse(
    int Id,
    string TargetType,
    string? TargetRole,
    string? GradeLevel,
    string? Section);

public record CoordinatorAnnouncementResponse(
    int Id,
    string Title,
    string Content,
    DateTime? ScheduledAt,
    DateTime? SentAt,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyCollection<CoordinatorAnnouncementRecipientResponse> Recipients);
