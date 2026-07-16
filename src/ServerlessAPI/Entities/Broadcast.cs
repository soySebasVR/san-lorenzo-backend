namespace ServerlessAPI.Entities;

public enum BroadcastAudience
{
    Students,
    Teachers,
    Parents,
    All,
}

/// <summary>
/// A school-wide communiqué the coordinator schedules. There is no delivery mechanism yet;
/// it is stored and listed.
/// </summary>
public class Broadcast
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public BroadcastAudience Audience { get; set; }

    /// <summary>Null means every grade.</summary>
    public string? GradeLevel { get; set; }

    public DateTime ScheduledFor { get; set; }
    public DateTime CreatedAt { get; set; }
}
