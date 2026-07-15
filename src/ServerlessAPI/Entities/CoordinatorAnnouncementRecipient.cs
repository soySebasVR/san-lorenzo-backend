namespace ServerlessAPI.Entities;

public class CoordinatorAnnouncementRecipient
{
    public int Id { get; set; }

    public int CoordinatorAnnouncementId { get; set; }

    public CoordinatorRecipientType TargetType { get; set; }

    public Role? TargetRole { get; set; }

    public string? GradeLevel { get; set; }

    public string? Section { get; set; }

    public CoordinatorAnnouncement Announcement { get; set; } = null!;
}
