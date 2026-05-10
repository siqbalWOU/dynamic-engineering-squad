namespace InfrastructureApp.Services.Minigames
{
    public static class MinigameConstants
    {
        public const int PointsPerGame = 5;

        public const string SlotsGameKey = "slots";
        public const string MatchingGameKey = "matching";
        public const string TriviaGameKey = "trivia";
        public const string TapRepairGameKey = "tap-repair";

        public static readonly string[] SupportedGameKeys =
        {
            SlotsGameKey,
            MatchingGameKey,
            TriviaGameKey,
            TapRepairGameKey
        };

        public static readonly string[] GenericCompletionGameKeys =
        {
            MatchingGameKey,
            TapRepairGameKey
        };

        public static readonly string[] SlotSymbols =
        {
            "pothole",
            "cone",
            "road-sign",
            "traffic-light",
            "bridge"
        };

        public static bool IsSupportedGameKey(string? gameKey)
            => !string.IsNullOrWhiteSpace(gameKey)
               && SupportedGameKeys.Contains(gameKey, System.StringComparer.OrdinalIgnoreCase);

        public static bool IsGenericCompletionGameKey(string? gameKey)
            => !string.IsNullOrWhiteSpace(gameKey)
               && GenericCompletionGameKeys.Contains(gameKey, System.StringComparer.OrdinalIgnoreCase);
    }
}
