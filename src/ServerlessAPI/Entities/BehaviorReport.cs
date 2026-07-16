namespace ServerlessAPI.Entities;

/// <summary>
/// A behavior note about a student. Minimal: the frontend has no capture screen yet, only
/// a summary chart on the coordinator dashboard.
/// </summary>
public class BehaviorReport
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public DateOnly Date { get; set; }
    public string Description { get; set; } = string.Empty;

    public Student Student { get; set; } = null!;
}
