namespace ServerlessAPI.Entities;

public class CoordinatorProfile
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string? Phone { get; set; }

    public string? ManagementArea { get; set; }

    public bool EmailNotifications { get; set; } = true;

    public bool AppNotifications { get; set; } = true;

    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
