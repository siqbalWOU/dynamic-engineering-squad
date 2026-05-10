using InfrastructureApp.ViewModels.Minigames;

namespace InfrastructureApp.Services.Minigames
{
    public interface IMinigameViewModelFactory
    {
        Task<MinigamesIndexViewModel> CreateIndexViewModelAsync(string userId);

        Task<SlotsViewModel> CreateSlotsViewModelAsync(string userId);

        Task<MatchingViewModel> CreateMatchingViewModelAsync(string userId);

        Task<TapRepairViewModel> CreateTapRepairViewModelAsync(string userId);

        Task<TriviaViewModel> CreateTriviaViewModelAsync(string userId);

        TriviaQuestionViewModel CreateTriviaQuestionViewModel(TriviaQuestion question);
    }
}
