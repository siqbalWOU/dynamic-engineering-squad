using System;
using System.Collections.Generic;

namespace InfrastructureApp.ViewModels.Minigames
{
    public class TriviaViewModel
    {
        public int CurrentPoints { get; init; }

        public int DailyPointsEarned { get; init; }

        public bool HasReachedDailyLimit { get; init; }

        public int PointsAvailable { get; init; }

        public int CorrectAnswers { get; init; }

        public int CorrectAnswersToWin { get; init; }

        public bool IsRoundComplete { get; init; }

        public TriviaQuestionViewModel? CurrentQuestion { get; init; }
    }

    public class TriviaQuestionViewModel
    {
        public string QuestionId { get; init; } = string.Empty;

        public string Prompt { get; init; } = string.Empty;

        public string QuestionType { get; init; } = string.Empty;

        public string? TextPlaceholder { get; init; }

        public IReadOnlyList<TriviaOptionViewModel> Options { get; init; } = Array.Empty<TriviaOptionViewModel>();
    }

    public class TriviaOptionViewModel
    {
        public string OptionKey { get; init; } = string.Empty;

        public string Label { get; init; } = string.Empty;
    }
}
