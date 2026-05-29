namespace InfrastructureApp.Services.Minigames
{
    public sealed class MinigameCatalogEntry
    {
        public string GameKey { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public string? PlayUrl { get; init; }

        public string ImageUrl { get; init; } = string.Empty;

        public string ImageAltText { get; init; } = string.Empty;

        public bool IsAvailable { get; init; }
    }

    public static class MinigameCatalog
    {
        public static readonly IReadOnlyList<MinigameCatalogEntry> Entries =
            new[]
            {
                new MinigameCatalogEntry
                {
                    GameKey = MinigameConstants.SlotsGameKey,
                    Name = "Slots",
                    Description = "Spin three infrastructure symbols.",
                    PlayUrl = "/Minigames/Slots",
                    ImageUrl = "/images/minigames/slots-card.svg",
                    ImageAltText = "Slot reels with roadwork icons",
                    IsAvailable = true
                },
                new MinigameCatalogEntry
                {
                    GameKey = MinigameConstants.MatchingGameKey,
                    Name = "Image Matching",
                    Description = "Match two images on a board of 12",
                    PlayUrl = "/Minigames/Matching",
                    ImageUrl = "/images/minigames/matching-card.svg",
                    ImageAltText = "Matching cards with infrastructure symbols",
                    IsAvailable = true
                },
                new MinigameCatalogEntry
                {
                    GameKey = MinigameConstants.TriviaGameKey,
                    Name = "Infrastructure Trivia",
                    Description = "Answer road-safety trivia questions.",
                    PlayUrl = "/Minigames/Trivia",
                    ImageUrl = "/images/minigames/trivia-card.svg",
                    ImageAltText = "Trivia card with road sign and question prompt",
                    IsAvailable = true
                },
                new MinigameCatalogEntry
                {
                    GameKey = MinigameConstants.TapRepairGameKey,
                    Name = "Tap Repair",
                    Description = "Repair as many potholes as you can before the timer runs out.",
                    PlayUrl = "/Minigames/TapRepair",
                    ImageUrl = "/images/minigames/tap-repair-card.svg",
                    ImageAltText = "Repair game card with pothole and repair marker",
                    IsAvailable = true
                }
            };

        public static MinigameCatalogEntry GetByGameKey(string gameKey)
        {
            return Entries.First(entry => entry.GameKey.Equals(gameKey, StringComparison.OrdinalIgnoreCase));
        }
    }
}
