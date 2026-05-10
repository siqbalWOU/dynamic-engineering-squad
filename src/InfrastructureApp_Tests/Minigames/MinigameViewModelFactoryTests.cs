using InfrastructureApp.Services.Minigames;
using InfrastructureApp.ViewModels.Minigames;
using Moq;

namespace InfrastructureApp_Tests.Minigames
{
    [TestFixture]
    public class MinigameViewModelFactoryTests
    {
        [Test]
        public async Task CreateIndexViewModelAsync_ReturnsAllCatalogGamesWithStatusData()
        {
            var serviceMock = new Mock<IMinigameService>();
            serviceMock
                .Setup(service => service.GetTodayStatusesAsync("user-1", null))
                .ReturnsAsync(new[]
                {
                    new MinigameStatus { GameKey = MinigameConstants.SlotsGameKey, DailyPointsEarned = 2, DailyPointsLimit = 5, HasReachedDailyLimit = false },
                    new MinigameStatus { GameKey = MinigameConstants.MatchingGameKey, DailyPointsEarned = 1, DailyPointsLimit = 5, HasReachedDailyLimit = false },
                    new MinigameStatus { GameKey = MinigameConstants.TriviaGameKey, DailyPointsEarned = 5, DailyPointsLimit = 5, HasReachedDailyLimit = true },
                    new MinigameStatus { GameKey = MinigameConstants.TapRepairGameKey, DailyPointsEarned = 0, DailyPointsLimit = 5, HasReachedDailyLimit = false }
                });
            serviceMock
                .Setup(service => service.GetCurrentPointsAsync("user-1"))
                .ReturnsAsync(42);

            var factory = new MinigameViewModelFactory(serviceMock.Object);

            var model = await factory.CreateIndexViewModelAsync("user-1");

            Assert.That(model.CurrentPoints, Is.EqualTo(42));
            Assert.That(model.Games.Select(game => game.GameKey), Is.EqualTo(new[]
            {
                MinigameConstants.SlotsGameKey,
                MinigameConstants.MatchingGameKey,
                MinigameConstants.TriviaGameKey,
                MinigameConstants.TapRepairGameKey
            }));
            Assert.That(model.Games.Single(game => game.GameKey == MinigameConstants.TriviaGameKey).HasReachedDailyLimit, Is.True);
            Assert.That(model.Games.Single(game => game.GameKey == MinigameConstants.MatchingGameKey).PlayUrl, Is.EqualTo("/Minigames/Matching"));
        }

        [Test]
        public async Task CreateMatchingViewModelAsync_UsesDefaultStatusWhenMissing()
        {
            var serviceMock = new Mock<IMinigameService>();
            serviceMock
                .Setup(service => service.GetTodayStatusesAsync("user-1", null))
                .ReturnsAsync(Array.Empty<MinigameStatus>());
            serviceMock
                .Setup(service => service.GetCurrentPointsAsync("user-1"))
                .ReturnsAsync(10);

            var factory = new MinigameViewModelFactory(serviceMock.Object);

            var model = await factory.CreateMatchingViewModelAsync("user-1");

            Assert.That(model.CurrentPoints, Is.EqualTo(10));
            Assert.That(model.DailyPointsEarned, Is.EqualTo(0));
            Assert.That(model.HasReachedDailyLimit, Is.False);
            Assert.That(model.PointsAvailable, Is.EqualTo(MinigameConstants.PointsPerGame));
        }

        [Test]
        public void CreateTriviaQuestionViewModel_MapsQuestionFieldsAndOptions()
        {
            var factory = new MinigameViewModelFactory(new Mock<IMinigameService>().Object);

            var question = new TriviaQuestion
            {
                QuestionId = "q1",
                Prompt = "Prompt",
                QuestionType = TriviaQuestionTypes.Dropdown,
                TextPlaceholder = "Type here",
                Options = new[]
                {
                    new TriviaOption { OptionKey = "a", Label = "Answer A" },
                    new TriviaOption { OptionKey = "b", Label = "Answer B" }
                }
            };

            var model = factory.CreateTriviaQuestionViewModel(question);

            Assert.That(model, Is.TypeOf<TriviaQuestionViewModel>());
            Assert.That(model.QuestionId, Is.EqualTo("q1"));
            Assert.That(model.QuestionType, Is.EqualTo(TriviaQuestionTypes.Dropdown));
            Assert.That(model.TextPlaceholder, Is.EqualTo("Type here"));
            Assert.That(model.Options.Select(option => option.OptionKey), Is.EqualTo(new[] { "a", "b" }));
        }
    }
}
