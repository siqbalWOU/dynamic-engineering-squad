using System;
using System.Collections.Generic;

namespace InfrastructureApp.Services.Minigames
{
    public static class TriviaQuestionTypes
    {
        public const string Radio = "radio";
        public const string TrueFalse = "true-false";
        public const string Dropdown = "dropdown";
        public const string Text = "text";
    }

    public class TriviaQuestion
    {
        public string QuestionId { get; init; } = string.Empty;

        public string Prompt { get; init; } = string.Empty;

        public string QuestionType { get; init; } = TriviaQuestionTypes.Radio;

        public string CorrectAnswerKey { get; init; } = string.Empty;

        public IReadOnlyList<string> AcceptedTextAnswers { get; init; } = Array.Empty<string>();

        public string? TextPlaceholder { get; init; }

        public IReadOnlyList<TriviaOption> Options { get; init; } = Array.Empty<TriviaOption>();
    }

    public class TriviaOption
    {
        public string OptionKey { get; init; } = string.Empty;

        public string Label { get; init; } = string.Empty;
    }

    public class TriviaAnswerSubmission
    {
        public string QuestionId { get; init; } = string.Empty;

        public string SelectedOptionKey { get; init; } = string.Empty;
    }

    public class TriviaRoundState
    {
        public string UserId { get; set; } = string.Empty;

        public string CurrentQuestionId { get; set; } = string.Empty;

        public List<string> RemainingQuestionIds { get; set; } = new();

        public int CorrectAnswers { get; set; }

        public int QuestionsAnswered { get; set; }

        public bool IsComplete { get; set; }
    }

    public class TriviaQuestionPromptResult
    {
        public TriviaQuestion CurrentQuestion { get; init; } = null!;

        public int CorrectAnswers { get; init; }

        public int CorrectAnswersToWin { get; init; }

        public int CurrentPoints { get; init; }

        public int DailyPointsEarned { get; init; }

        public int DailyPointsLimit { get; init; }

        public bool HasReachedDailyLimit { get; init; }

        public bool IsRoundComplete { get; init; }
    }

    public class TriviaAnswerResult
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

        public TriviaQuestion? NextQuestion { get; init; }

        public string ResultMessage { get; init; } = string.Empty;
    }
}
