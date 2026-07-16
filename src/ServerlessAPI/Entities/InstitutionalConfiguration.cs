namespace ServerlessAPI.Entities;

public class InstitutionalConfiguration
{
    public int Id { get; set; }

    public string InstitutionName { get; set; } = string.Empty;

    public int AcademicYear { get; set; }

    public string AcademicPeriod { get; set; } = string.Empty;

    public int AttendanceToleranceMinutes { get; set; }

    public decimal AbsenceAlertPercentage { get; set; }

    public string TimeZone { get; set; } = "America/Lima";

    public DateTime UpdatedAt { get; set; }

    public int UpdatedByUserId { get; set; }

    public User UpdatedByUser { get; set; } = null!;
}
