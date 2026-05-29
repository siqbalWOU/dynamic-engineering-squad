namespace InfrastructureApp.ViewModels.Minigames
{
    public class TriviaAnswerResultViewModel
    {
        public bool WasCorrect { get; init; }

        public int CorrectAnswers { get; init; }

        public int CorrectAnswersToWin { get; init; }

        public bool IsRoundComplete { get; init; }

        public int AwardedPoints { get; init; }

        public int CurrentPoints { get; init; }

        public int DailyPointsEarned { get; init; }

        public int DailyPointsLimit { get; init; }

        public bool HasReachedDailyLimit { get; init; }

        public string ResultMessage { get; init; } = string.Empty;

        public TriviaQuestionViewModel? NextQuestion { get; init; }
    }
}
