namespace ServerlessAPI.Entities;

/// <summary>
/// School-wide settings the coordinator edits. Single row (Id = 1); there is only one
/// school in this system.
/// </summary>
public class SystemSettings
{
    public int Id { get; set; }

    public string SchoolName { get; set; } = string.Empty;
    public int AcademicYear { get; set; }

    /// <summary>Current grading term, e.g. "2026-I". Used as the default term elsewhere.</summary>
    public string CurrentTerm { get; set; } = string.Empty;

    public int UnjustifiedAbsenceThreshold { get; set; }
    public int LatenessToleranceMinutes { get; set; }
}
