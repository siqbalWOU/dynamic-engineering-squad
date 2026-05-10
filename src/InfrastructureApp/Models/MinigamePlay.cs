using System;

namespace InfrastructureApp.Models
{
    public class MinigamePlay
    {
        public int Id { get; set; }

        public string UserId { get; set; } = null!;

        public string GameKey { get; set; } = null!;

        public DateTime PlayedOnDate { get; set; }

        public int PointsAwarded { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
