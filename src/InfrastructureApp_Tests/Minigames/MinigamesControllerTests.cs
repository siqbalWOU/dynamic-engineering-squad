using System.Security.Claims;
using InfrastructureApp.Controllers;
using InfrastructureApp.Models;
using InfrastructureApp.Services.Minigames;
using InfrastructureApp.ViewModels.Minigames;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InfrastructureApp_Tests.Minigames
{
    [TestFixture]
    public class MinigamesControllerTests
    {
        [Test]
        public async Task Index_ReturnsViewResult_WithGameCards()
        {
            var serviceMock = new Mock<IMinigameService>();
            var factory = new MinigameViewModelFactory(serviceMock.Object);
            serviceMock
                .Setup(service => service.GetTodayStatusesAsync("user-1", null))
                .ReturnsAsync(new[]
                {
                    new MinigameStatus { GameKey = MinigameConstants.SlotsGameKey, DailyPointsEarned = 2, DailyPointsLimit = 5, HasReachedDailyLimit = false },
                    new MinigameStatus { GameKey = MinigameConstants.MatchingGameKey, DailyPointsEarned = 0, DailyPointsLimit = 5, HasReachedDailyLimit = false },
                    new MinigameStatus { GameKey = MinigameConstants.TriviaGameKey, DailyPointsEarned = 0, DailyPointsLimit = 5, HasReachedDailyLimit = false },
                    new MinigameStatus { GameKey = MinigameConstants.TapRepairGameKey, DailyPointsEarned = 0, DailyPointsLimit = 5, HasReachedDailyLimit = false }
                });
            serviceMock
                .Setup(service => service.GetCurrentPointsAsync("user-1"))
                .ReturnsAsync(25);
            var auditLogService = new Mock<InfrastructureApp.Services.IAuditLogService>();

            var controller = new MinigamesController(serviceMock.Object, factory, CreateUserManager(), auditLogService.Object)
            {
                ControllerContext = BuildControllerContext("user-1")
            };

            var result = await controller.Index();

            Assert.That(result, Is.TypeOf<ViewResult>());

            var viewResult = (ViewResult)result;
            Assert.That(viewResult.Model, Is.TypeOf<MinigamesIndexViewModel>());

            var model = (MinigamesIndexViewModel)viewResult.Model!;
            Assert.That(model.CurrentPoints, Is.EqualTo(25));
            Assert.That(model.Games.Count, Is.EqualTo(4));
            Assert.That(model.Games.Single(game => game.GameKey == MinigameConstants.SlotsGameKey).IsAvailable, Is.True);
            Assert.That(model.Games.Single(game => game.GameKey == MinigameConstants.SlotsGameKey).DailyPointsEarned, Is.EqualTo(2));
            Assert.That(model.Games.Single(game => game.GameKey == MinigameConstants.MatchingGameKey).IsAvailable, Is.True);
            Assert.That(model.Games.Single(game => game.GameKey == MinigameConstants.TriviaGameKey).IsAvailable, Is.True);
            Assert.That(model.Games.Single(game => game.GameKey == MinigameConstants.TapRepairGameKey).IsAvailable, Is.True);
        }

        [Test]
        public async Task Matching_ReturnsViewResult_WithMatchingViewModel()
        {
            var serviceMock = new Mock<IMinigameService>();
            var factory = new MinigameViewModelFactory(serviceMock.Object);
            serviceMock
                .Setup(service => service.GetTodayStatusesAsync("user-1", null))
                .ReturnsAsync(new[]
                {
                    new MinigameStatus { GameKey = MinigameConstants.MatchingGameKey, DailyPointsEarned = 0, DailyPointsLimit = 5, HasReachedDailyLimit = false }
                });
            serviceMock
                .Setup(service => service.GetCurrentPointsAsync("user-1"))
                .ReturnsAsync(31);
            var auditLogService = new Mock<InfrastructureApp.Services.IAuditLogService>();

            var controller = new MinigamesController(serviceMock.Object, factory, CreateUserManager(), auditLogService.Object)
            {
                ControllerContext = BuildControllerContext("user-1")
            };

            var result = await controller.Matching();

            Assert.That(result, Is.TypeOf<ViewResult>());

            var viewResult = (ViewResult)result;
            Assert.That(viewResult.Model, Is.TypeOf<MatchingViewModel>());

            var model = (MatchingViewModel)viewResult.Model!;
            Assert.That(model.CurrentPoints, Is.EqualTo(31));
            Assert.That(model.HasReachedDailyLimit, Is.False);
        }

        [Test]
        public async Task Trivia_ReturnsViewResult_WithTriviaViewModel()
        {
            var serviceMock = new Mock<IMinigameService>();
            var factory = new MinigameViewModelFactory(serviceMock.Object);
            serviceMock
                .Setup(service => service.GetOrStartTriviaRoundAsync("user-1", null))
                .ReturnsAsync(new TriviaQuestionPromptResult
                {
                    CurrentQuestion = new TriviaQuestion
                    {
                        QuestionId = "q1",
                        Prompt = "Question?",
                        QuestionType = TriviaQuestionTypes.Radio,
                        CorrectAnswerKey = "a",
                        Options = new[]
                        {
                            new TriviaOption { OptionKey = "a", Label = "Answer A" },
                            new TriviaOption { OptionKey = "b", Label = "Answer B" }
                        }
                    },
                    CorrectAnswers = 2,
                    CorrectAnswersToWin = 10,
                    CurrentPoints = 22,
                    DailyPointsEarned = 0,
                    DailyPointsLimit = 5,
                    HasReachedDailyLimit = false,
                    IsRoundComplete = false
                });
            var auditLogService = new Mock<InfrastructureApp.Services.IAuditLogService>();

            var controller = new MinigamesController(serviceMock.Object, factory, CreateUserManager(), auditLogService.Object)
            {
                ControllerContext = BuildControllerContext("user-1")
            };

            var result = await controller.Trivia();

            Assert.That(result, Is.TypeOf<ViewResult>());
            var model = (TriviaViewModel)((ViewResult)result).Model!;
            Assert.That(model.CurrentPoints, Is.EqualTo(22));
            Assert.That(model.CorrectAnswers, Is.EqualTo(2));
            Assert.That(model.CurrentQuestion, Is.Not.Null);
            Assert.That(model.CurrentQuestion!.QuestionId, Is.EqualTo("q1"));
        }

        [Test]
        public async Task TapRepair_ReturnsViewResult_WithTapRepairViewModel()
        {
            var serviceMock = new Mock<IMinigameService>();
            var factory = new MinigameViewModelFactory(serviceMock.Object);
            serviceMock
                .Setup(service => service.GetTodayStatusesAsync("user-1", null))
                .ReturnsAsync(new[]
                {
                    new MinigameStatus
                    {
                        GameKey = MinigameConstants.TapRepairGameKey,
                        DailyPointsEarned = 0,
                        DailyPointsLimit = 5,
                        HasReachedDailyLimit = false
                    }
                });
            serviceMock
                .Setup(service => service.GetCurrentPointsAsync("user-1"))
                .ReturnsAsync(19);
            var auditLogService = new Mock<InfrastructureApp.Services.IAuditLogService>();

            var controller = new MinigamesController(serviceMock.Object, factory, CreateUserManager(), auditLogService.Object)
            {
                ControllerContext = BuildControllerContext("user-1")
            };

            var result = await controller.TapRepair();

            Assert.That(result, Is.TypeOf<ViewResult>());
            var model = (TapRepairViewModel)((ViewResult)result).Model!;
            Assert.That(model.CurrentPoints, Is.EqualTo(19));
            Assert.That(model.HasReachedDailyLimit, Is.False);
            Assert.That(model.PointsAvailable, Is.EqualTo(5));
        }

        [Test]
        public async Task SpinSlots_ReturnsJsonWithServerGeneratedSymbols()
        {
            var serviceMock = new Mock<IMinigameService>();
            var factory = new Mock<IMinigameViewModelFactory>();
            serviceMock
                .Setup(service => service.SpinSlotsAsync("user-1", null))
                .ReturnsAsync(new SlotsSpinResult
                {
                    GameKey = MinigameConstants.SlotsGameKey,
                    AwardedPoints = 1,
                    CurrentPoints = 15,
                    DailyPointsEarned = 3,
                    DailyPointsLimit = 5,
                    HasReachedDailyLimit = false,
                    Symbols = new[] { "cone", "bridge", "pothole" },
                    IsWinningSpin = true,
                    ResultLabel = "Three of a Kind"
                });
            var auditLogService = new Mock<InfrastructureApp.Services.IAuditLogService>();

            var controller = new MinigamesController(serviceMock.Object, factory.Object, CreateUserManager(), auditLogService.Object)
            {
                ControllerContext = BuildControllerContext("user-1")
            };

            var result = await controller.SpinSlots();

            Assert.That(result, Is.TypeOf<JsonResult>());

            var jsonResult = (JsonResult)result;
            Assert.That(jsonResult.Value, Is.TypeOf<GameCompletionResultViewModel>());

            var payload = (GameCompletionResultViewModel)jsonResult.Value!;
            Assert.That(payload.Symbols, Is.EquivalentTo(new[] { "cone", "bridge", "pothole" }));
            Assert.That(payload.AwardedPoints, Is.EqualTo(1));
        }

        [Test]
        public async Task SpinSlots_WhenDailyCapReached_ReturnsNoAdditionalPoints()
        {
            var serviceMock = new Mock<IMinigameService>();
            var factory = new Mock<IMinigameViewModelFactory>();
            serviceMock
                .Setup(service => service.SpinSlotsAsync("user-1", null))
                .ReturnsAsync(new SlotsSpinResult
                {
                    GameKey = MinigameConstants.SlotsGameKey,
                    AwardedPoints = 0,
                    CurrentPoints = 15,
                    DailyPointsEarned = 5,
                    DailyPointsLimit = 5,
                    HasReachedDailyLimit = true,
                    Symbols = new[] { "cone", "bridge", "pothole" },
                    IsWinningSpin = false,
                    ResultLabel = "Road Crew Spin"
                });
            var auditLogService = new Mock<InfrastructureApp.Services.IAuditLogService>();

            var controller = new MinigamesController(serviceMock.Object, factory.Object, CreateUserManager(), auditLogService.Object)
            {
                ControllerContext = BuildControllerContext("user-1")
            };

            var result = await controller.SpinSlots();
            var payload = (GameCompletionResultViewModel)((JsonResult)result).Value!;

            Assert.That(payload.AwardedPoints, Is.EqualTo(0));
            Assert.That(payload.HasReachedDailyLimit, Is.True);
        }

        [Test]
        public async Task CompleteGame_WithInvalidGameKey_ReturnsBadRequest()
        {
            var serviceMock = new Mock<IMinigameService>();
            var factory = new Mock<IMinigameViewModelFactory>();
            var auditLogService = new Mock<InfrastructureApp.Services.IAuditLogService>();
            var controller = new MinigamesController(serviceMock.Object, factory.Object, CreateUserManager(), auditLogService.Object)
            {
                ControllerContext = BuildControllerContext("user-1")
            };

            var result = await controller.CompleteGame(new CompleteGameRequestViewModel
            {
                GameKey = "not-a-game"
            });

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task CompleteGame_WithSlotsGameKey_ReturnsBadRequest()
        {
            var serviceMock = new Mock<IMinigameService>();
            var factory = new Mock<IMinigameViewModelFactory>();
            var auditLogService = new Mock<InfrastructureApp.Services.IAuditLogService>();
            var controller = new MinigamesController(serviceMock.Object, factory.Object, CreateUserManager(), auditLogService.Object)
            {
                ControllerContext = BuildControllerContext("user-1")
            };

            var result = await controller.CompleteGame(new CompleteGameRequestViewModel
            {
                GameKey = MinigameConstants.SlotsGameKey
            });

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task CompleteGame_ForMatching_ReturnsCompletionPayload()
        {
            var serviceMock = new Mock<IMinigameService>();
            var factory = new Mock<IMinigameViewModelFactory>();
            serviceMock
                .Setup(service => service.CompleteGameAsync("user-1", MinigameConstants.MatchingGameKey, null))
                .ReturnsAsync(new MinigameAwardResult
                {
                    GameKey = MinigameConstants.MatchingGameKey,
                    AwardedPoints = 1,
                    CurrentPoints = 36,
                    DailyPointsEarned = 1,
                    DailyPointsLimit = 5,
                    HasReachedDailyLimit = false
                });
            var auditLogService = new Mock<InfrastructureApp.Services.IAuditLogService>();

            var controller = new MinigamesController(serviceMock.Object, factory.Object, CreateUserManager(), auditLogService.Object)
            {
                ControllerContext = BuildControllerContext("user-1")
            };

            var result = await controller.CompleteGame(new CompleteGameRequestViewModel
            {
                GameKey = MinigameConstants.MatchingGameKey
            });

            Assert.That(result, Is.TypeOf<JsonResult>());

            var payload = (GameCompletionResultViewModel)((JsonResult)result).Value!;
            Assert.That(payload.GameKey, Is.EqualTo(MinigameConstants.MatchingGameKey));
            Assert.That(payload.AwardedPoints, Is.EqualTo(1));
            Assert.That(payload.HasReachedDailyLimit, Is.False);
        }

        [Test]
        public async Task CompleteGame_ForTapRepair_ReturnsCompletionPayload()
        {
            var serviceMock = new Mock<IMinigameService>();
            var factory = new Mock<IMinigameViewModelFactory>();
            serviceMock
                .Setup(service => service.CompleteGameAsync("user-1", MinigameConstants.TapRepairGameKey, null))
                .ReturnsAsync(new MinigameAwardResult
                {
                    GameKey = MinigameConstants.TapRepairGameKey,
                    AwardedPoints = 1,
                    CurrentPoints = 37,
                    DailyPointsEarned = 3,
                    DailyPointsLimit = 5,
                    HasReachedDailyLimit = false
                });
            var auditLogService = new Mock<InfrastructureApp.Services.IAuditLogService>();

            var controller = new MinigamesController(serviceMock.Object, factory.Object, CreateUserManager(), auditLogService.Object)
            {
                ControllerContext = BuildControllerContext("user-1")
            };

            var result = await controller.CompleteGame(new CompleteGameRequestViewModel
            {
                GameKey = MinigameConstants.TapRepairGameKey
            });

            Assert.That(result, Is.TypeOf<JsonResult>());

            var payload = (GameCompletionResultViewModel)((JsonResult)result).Value!;
            Assert.That(payload.GameKey, Is.EqualTo(MinigameConstants.TapRepairGameKey));
            Assert.That(payload.AwardedPoints, Is.EqualTo(1));
            Assert.That(payload.HasReachedDailyLimit, Is.False);
        }

        [Test]
        public async Task SubmitTrivia_WithInvalidPayload_ReturnsBadRequest()
        {
            var serviceMock = new Mock<IMinigameService>();
            var factory = new Mock<IMinigameViewModelFactory>();
            var auditLogService = new Mock<InfrastructureApp.Services.IAuditLogService>();
            serviceMock
                .Setup(service => service.SubmitTriviaAnswerAsync("user-1", It.IsAny<TriviaAnswerSubmission>(), null))
                .ThrowsAsync(new ArgumentException("Trivia answer is incomplete."));

            var controller = new MinigamesController(serviceMock.Object, factory.Object, CreateUserManager(), auditLogService.Object)
            {
                ControllerContext = BuildControllerContext("user-1")
            };

            var result = await controller.SubmitTrivia(new SubmitTriviaRequestViewModel());

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task SubmitTrivia_ReturnsTriviaResultPayload()
        {
            var serviceMock = new Mock<IMinigameService>();
            var factory = new MinigameViewModelFactory(serviceMock.Object);
            var auditLogService = new Mock<InfrastructureApp.Services.IAuditLogService>();
            serviceMock
                .Setup(service => service.SubmitTriviaAnswerAsync(
                    "user-1",
                    It.Is<TriviaAnswerSubmission>(answer => answer.QuestionId == "q1" && answer.SelectedOptionKey == "a"),
                    null))
                .ReturnsAsync(new TriviaAnswerResult
                {
                    WasCorrect = true,
                    CorrectAnswers = 3,
                    CorrectAnswersToWin = 10,
                    IsRoundComplete = false,
                    AwardedPoints = 0,
                    CurrentPoints = 50,
                    DailyPointsEarned = 0,
                    DailyPointsLimit = 5,
                    HasReachedDailyLimit = false,
                    NextQuestion = new TriviaQuestion
                    {
                        QuestionId = "q2",
                        Prompt = "Next?",
                        QuestionType = TriviaQuestionTypes.Dropdown,
                        Options = new[]
                        {
                            new TriviaOption { OptionKey = "x", Label = "X" },
                            new TriviaOption { OptionKey = "y", Label = "Y" }
                        }
                    }
                });

            var controller = new MinigamesController(serviceMock.Object, factory, CreateUserManager(), auditLogService.Object)
            {
                ControllerContext = BuildControllerContext("user-1")
            };

            var result = await controller.SubmitTrivia(new SubmitTriviaRequestViewModel
            {
                QuestionId = "q1",
                SelectedOptionKey = "a"
            });

            Assert.That(result, Is.TypeOf<JsonResult>());

            var payload = (GameCompletionResultViewModel)((JsonResult)result).Value!;
            Assert.That(payload.GameKey, Is.EqualTo(MinigameConstants.TriviaGameKey));
            Assert.That(payload.WasCorrect, Is.True);
            Assert.That(payload.CorrectAnswers, Is.EqualTo(3));
            Assert.That(payload.CorrectAnswersToWin, Is.EqualTo(10));
            Assert.That(payload.NextQuestion, Is.Not.Null);
            Assert.That(payload.NextQuestion!.QuestionId, Is.EqualTo("q2"));
        }

        private static UserManager<Users> CreateUserManager()
        {
            var store = new Mock<IUserStore<Users>>();
            return new UserManager<Users>(
                store.Object,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);
        }

        private static ControllerContext BuildControllerContext(string userId)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, "minigame-user")
            }, "TestAuth");

            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity),
                    Session = new TestSession()
                }
            };
        }
    }
}
