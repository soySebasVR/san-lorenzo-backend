using System.ComponentModel.DataAnnotations;

namespace ServerlessAPI.Dtos;

public record InstitutionalConfigurationResponse(
    int Id,
    string InstitutionName,
    int AcademicYear,
    string AcademicPeriod,
    int AttendanceToleranceMinutes,
    decimal AbsenceAlertPercentage,
    string TimeZone,
    DateTime UpdatedAt);

public record UpdateInstitutionalConfigurationRequest
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string InstitutionName { get; init; } = string.Empty;

    [Range(2000, 2100)]
    public int AcademicYear { get; init; }

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string AcademicPeriod { get; init; } = string.Empty;

    [Range(0, 120)]
    public int AttendanceToleranceMinutes { get; init; }

    [Range(typeof(decimal), "0", "100")]
    public decimal AbsenceAlertPercentage { get; init; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string TimeZone { get; init; } = "America/Lima";
}
