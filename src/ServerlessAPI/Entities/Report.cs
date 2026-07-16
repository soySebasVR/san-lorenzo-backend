namespace ServerlessAPI.Entities;

/// <summary>
/// A generated averages report. Only the filters and timestamp are stored; the text file
/// is rebuilt from current grades on download.
/// </summary>
public class Report
{
    public int Id { get; set; }
    public string GradeLevel { get; set; } = string.Empty;
    public string Term { get; set; } = string.Empty;

    /// <summary>Null means "all teachers of the grade".</summary>
    public int? TeacherId { get; set; }

    public DateTime GeneratedAt { get; set; }

    public Teacher? Teacher { get; set; }
}
