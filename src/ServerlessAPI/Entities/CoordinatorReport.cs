namespace ServerlessAPI.Entities;

public class CoordinatorReport
{
    public int Id { get; set; }

    public CoordinatorReportType ReportType { get; set; }

    public string FiltersJson { get; set; } = "{}";

    public string ResultJson { get; set; } = "{}";

    public int GeneratedByUserId { get; set; }

    public DateTime GeneratedAt { get; set; }

    public User GeneratedByUser { get; set; } = null!;
}
