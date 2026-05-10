namespace InfrastructureApp.ViewModels.Minigames
{
    public class SlotsViewModel
    {
        public int CurrentPoints { get; init; }

        public int PointsAvailable { get; init; }

        public int DailyPointsEarned { get; init; }

        public bool HasReachedDailyLimit { get; init; }
    }
}
