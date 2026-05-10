using InfrastructureApp.ViewModels.Minigames;

namespace InfrastructureApp.Services.Minigames
{
    public class MinigameViewModelFactory : IMinigameViewModelFactory
    {
        private readonly IMinigameService _minigameService;

        public MinigameViewModelFactory(IMinigameService minigameService)
        {
            _minigameService = minigameService;
        }

        public async Task<MinigamesIndexViewModel> CreateIndexViewModelAsync(string userId)
        {
            var statuses = await _minigameService.GetTodayStatusesAsync(userId);
            var currentPoints = await _minigameService.GetCurrentPointsAsync(userId);
            var statusMap = statuses.ToDictionary(status => status.GameKey, status => status, StringComparer.OrdinalIgnoreCase);

            return new MinigamesIndexViewModel
            {
                CurrentPoints = currentPoints,
                Games = MinigameCatalog.Entries
                    .Select(entry => BuildCard(entry, statusMap))
                    .ToArray()
            };
        }

        public Task<SlotsViewModel> CreateSlotsViewModelAsync(string userId)
            => CreateStatusBackedViewModelAsync(
                userId,
                MinigameConstants.SlotsGameKey,
                status => new SlotsViewModel
                {
                    CurrentPoints = status.CurrentPoints,
                    DailyPointsEarned = status.GameStatus.DailyPointsEarned,
                    HasReachedDailyLimit = status.GameStatus.HasReachedDailyLimit,
                    PointsAvailable = MinigameConstants.PointsPerGame
                });

        public Task<MatchingViewModel> CreateMatchingViewModelAsync(string userId)
            => CreateStatusBackedViewModelAsync(
                userId,
                MinigameConstants.MatchingGameKey,
                status => new MatchingViewModel
                {
                    CurrentPoints = status.CurrentPoints,
                    DailyPointsEarned = status.GameStatus.DailyPointsEarned,
                    HasReachedDailyLimit = status.GameStatus.HasReachedDailyLimit,
                    PointsAvailable = MinigameConstants.PointsPerGame
                });

        public Task<TapRepairViewModel> CreateTapRepairViewModelAsync(string userId)
            => CreateStatusBackedViewModelAsync(
                userId,
                MinigameConstants.TapRepairGameKey,
                status => new TapRepairViewModel
                {
                    CurrentPoints = status.CurrentPoints,
                    DailyPointsEarned = status.GameStatus.DailyPointsEarned,
                    HasReachedDailyLimit = status.GameStatus.HasReachedDailyLimit,
                    PointsAvailable = MinigameConstants.PointsPerGame
                });

        public async Task<TriviaViewModel> CreateTriviaViewModelAsync(string userId)
        {
            var triviaRound = await _minigameService.GetOrStartTriviaRoundAsync(userId);

            return new TriviaViewModel
            {
                CurrentPoints = triviaRound.CurrentPoints,
                DailyPointsEarned = triviaRound.DailyPointsEarned,
                HasReachedDailyLimit = triviaRound.HasReachedDailyLimit,
                PointsAvailable = MinigameConstants.PointsPerGame,
                CorrectAnswers = triviaRound.CorrectAnswers,
                CorrectAnswersToWin = triviaRound.CorrectAnswersToWin,
                IsRoundComplete = triviaRound.IsRoundComplete,
                CurrentQuestion = triviaRound.CurrentQuestion == null
                    ? null
                    : CreateTriviaQuestionViewModel(triviaRound.CurrentQuestion)
            };
        }

        public TriviaQuestionViewModel CreateTriviaQuestionViewModel(TriviaQuestion question)
        {
            return new TriviaQuestionViewModel
            {
                QuestionId = question.QuestionId,
                Prompt = question.Prompt,
                QuestionType = question.QuestionType,
                TextPlaceholder = question.TextPlaceholder,
                Options = question.Options
                    .Select(option => new TriviaOptionViewModel
                    {
                        OptionKey = option.OptionKey,
                        Label = option.Label
                    })
                    .ToList()
            };
        }

        private async Task<TViewModel> CreateStatusBackedViewModelAsync<TViewModel>(
            string userId,
            string gameKey,
            Func<StatusBackedFactoryData, TViewModel> buildViewModel)
        {
            var statuses = await _minigameService.GetTodayStatusesAsync(userId);
            var currentPoints = await _minigameService.GetCurrentPointsAsync(userId);
            var gameStatus = statuses.FirstOrDefault(status => status.GameKey == gameKey)
                ?? new MinigameStatus
                {
                    GameKey = gameKey,
                    DailyPointsLimit = MinigameConstants.PointsPerGame
                };

            return buildViewModel(new StatusBackedFactoryData
            {
                CurrentPoints = currentPoints,
                GameStatus = gameStatus
            });
        }

        private static MinigameCardViewModel BuildCard(
            MinigameCatalogEntry entry,
            IReadOnlyDictionary<string, MinigameStatus> statusMap)
        {
            var status = statusMap.TryGetValue(entry.GameKey, out var gameStatus)
                ? gameStatus
                : new MinigameStatus
                {
                    GameKey = entry.GameKey,
                    DailyPointsLimit = MinigameConstants.PointsPerGame
                };

            return new MinigameCardViewModel
            {
                GameKey = entry.GameKey,
                Name = entry.Name,
                Description = entry.Description,
                PointsAvailable = MinigameConstants.PointsPerGame,
                DailyPointsEarned = status.DailyPointsEarned,
                HasReachedDailyLimit = status.HasReachedDailyLimit,
                IsAvailable = entry.IsAvailable,
                PlayUrl = entry.PlayUrl,
                ImageUrl = entry.ImageUrl,
                ImageAltText = entry.ImageAltText
            };
        }

        private sealed class StatusBackedFactoryData
        {
            public int CurrentPoints { get; init; }

            public MinigameStatus GameStatus { get; init; } = null!;
        }
    }
}
