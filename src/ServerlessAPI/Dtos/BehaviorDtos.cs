using System.ComponentModel.DataAnnotations;

namespace ServerlessAPI.Dtos;

public record CreateBehaviorReportRequest
{
    [Range(1, int.MaxValue)]
    public int StudentId { get; init; }

    [Required]
    public DateOnly Date { get; init; }

    [Required, StringLength(500, MinimumLength = 1)]
    public string Description { get; init; } = string.Empty;
}

public record BehaviorReportResponse(
    int Id,
    int StudentId,
    string StudentName,
    DateOnly Date,
    string Description);
