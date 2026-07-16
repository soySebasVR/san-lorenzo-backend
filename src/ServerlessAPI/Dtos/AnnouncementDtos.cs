using System.ComponentModel.DataAnnotations;

namespace ServerlessAPI.Dtos;

public record SendAnnouncementRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Subject { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string Message { get; init; } = string.Empty;

    // Filtros opcionales (null ignora el filtro).
    [StringLength(20)] public string? Section { get; init; }
    [StringLength(20)] public string? GradeLevel { get; init; }
    public int? CourseId { get; init; }
}

public record AnnouncementResponse(
    int Id,
    string Subject,
    int RecipientCount,
    DateTime SentAt);
