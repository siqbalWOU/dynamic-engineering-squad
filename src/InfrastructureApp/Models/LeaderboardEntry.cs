namespace InfrastructureApp.Models;

public class LeaderboardEntry
{
    public string UserId { get; set; } = "";
    public int UserPoints { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public string AvatarUrl { get; set; } = "";
}
