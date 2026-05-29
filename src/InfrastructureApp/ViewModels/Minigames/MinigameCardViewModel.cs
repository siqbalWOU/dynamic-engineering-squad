namespace InfrastructureApp.ViewModels.Minigames
{
    public class MinigameCardViewModel
    {
        public string GameKey { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public int PointsAvailable { get; init; }

        public int DailyPointsEarned { get; init; }

        public bool HasReachedDailyLimit { get; init; }

        public bool IsAvailable { get; init; }

        public string? PlayUrl { get; init; }

        public string ImageUrl { get; init; } = string.Empty;

        public string ImageAltText { get; init; } = string.Empty;
    }
}
