namespace ServerlessAPI.Entities;

public class CoordinatorAnnouncement
{
    public int Id { get; set; }

    public int CreatedByUserId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime? ScheduledAt { get; set; }

    public DateTime? SentAt { get; set; }

    public CoordinatorAnnouncementStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public User CreatedByUser { get; set; } = null!;

    public ICollection<CoordinatorAnnouncementRecipient> Recipients { get; set; }
        = new List<CoordinatorAnnouncementRecipient>();
}
