using InfrastructureApp.Data;
using InfrastructureApp.Models;
using InfrastructureApp.Services.Minigames;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace InfrastructureApp_Tests.Minigames
{
    [TestFixture]
    public class MinigameServiceTests
    {
        private ApplicationDbContext _db = null!;
        private DefaultHttpContext _httpContext = null!;
        private IHttpContextAccessor _httpContextAccessor = null!;
        private MinigameService _service = null!;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("MinigameServiceTest_" + Guid.NewGuid())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            _db = new ApplicationDbContext(options);
            _httpContext = new DefaultHttpContext();
            _httpContext.Session = new TestSession();
            _httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = _httpContext
            };

            _service = new MinigameService(_db, _httpContextAccessor);
        }

        [TearDown]
        public void TearDown()
        {
            _db.Dispose();
        }

        [Test]
        public async Task CompleteGameAsync_MatchingAwardsOnePointPerBoardClear()
        {
            var result = await _service.CompleteGameAsync("user-1", MinigameConstants.MatchingGameKey, new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc));

            Assert.That(result.AwardedPoints, Is.EqualTo(1));
            Assert.That(result.DailyPointsEarned, Is.EqualTo(1));
            Assert.That(result.HasReachedDailyLimit, Is.False);
        }

        [Test]
        public async Task CompleteGameAsync_MatchingCanBeCompletedMultipleTimesUntilDailyCap()
        {
            var date = new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc);

            var first = await _service.CompleteGameAsync("user-1", MinigameConstants.MatchingGameKey, date);
            var second = await _service.CompleteGameAsync("user-1", MinigameConstants.MatchingGameKey, date.AddHours(2));

            Assert.That(first.AwardedPoints, Is.EqualTo(1));
            Assert.That(second.AwardedPoints, Is.EqualTo(1));
            Assert.That(second.DailyPointsEarned, Is.EqualTo(2));
            Assert.That(second.HasReachedDailyLimit, Is.False);

            var points = await _db.UserPoints.SingleAsync(x => x.UserId == "user-1");
            Assert.That(points.CurrentPoints, Is.EqualTo(2));
            Assert.That(points.LifetimePoints, Is.EqualTo(2));
        }

        [Test]
        public async Task CompleteGameAsync_CompletingDifferentGamesOnSameDayAwardsEachGame()
        {
            var date = new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc);

            await _service.CompleteGameAsync("user-1", MinigameConstants.MatchingGameKey, date);
            await _service.CompleteGameAsync("user-1", MinigameConstants.TriviaGameKey, date);

            var points = await _db.UserPoints.SingleAsync(x => x.UserId == "user-1");
            Assert.That(points.CurrentPoints, Is.EqualTo(6));
            Assert.That(points.LifetimePoints, Is.EqualTo(6));
            Assert.That(await _db.MinigamePlays.CountAsync(), Is.EqualTo(2));
        }

        [Test]
        public async Task CompleteGameAsync_CompletingSameGameOnDifferentDayAwardsAgain()
        {
            await _service.CompleteGameAsync("user-1", MinigameConstants.TriviaGameKey, new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc));
            var second = await _service.CompleteGameAsync("user-1", MinigameConstants.TriviaGameKey, new DateTime(2026, 4, 29, 10, 0, 0, DateTimeKind.Utc));

            Assert.That(second.AwardedPoints, Is.EqualTo(5));

            var points = await _db.UserPoints.SingleAsync(x => x.UserId == "user-1");
            Assert.That(points.CurrentPoints, Is.EqualTo(10));
            Assert.That(points.LifetimePoints, Is.EqualTo(10));
        }

        [Test]
        public async Task GetOrStartTriviaRoundAsync_ReturnsOneCurrentQuestion()
        {
            var result = await _service.GetOrStartTriviaRoundAsync("user-1", new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc));

            Assert.That(result.CurrentQuestion, Is.Not.Null);
            Assert.That(result.CurrentQuestion.QuestionId, Is.Not.Empty);
            Assert.That(result.CorrectAnswers, Is.EqualTo(0));
            Assert.That(result.CorrectAnswersToWin, Is.EqualTo(10));
            Assert.That(result.IsRoundComplete, Is.False);
        }

        [Test]
        public async Task SubmitTriviaAnswerAsync_CorrectAnswerAdvancesToNextQuestionWithoutAwardingUntilTenCorrect()
        {
            var round = await _service.GetOrStartTriviaRoundAsync("user-1", new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc));

            var result = await _service.SubmitTriviaAnswerAsync(
                "user-1",
                new TriviaAnswerSubmission
                {
                    QuestionId = round.CurrentQuestion.QuestionId,
                    SelectedOptionKey = round.CurrentQuestion.CorrectAnswerKey
                },
                new DateTime(2026, 4, 28, 10, 1, 0, DateTimeKind.Utc));

            Assert.That(result.WasCorrect, Is.True);
            Assert.That(result.CorrectAnswers, Is.EqualTo(1));
            Assert.That(result.AwardedPoints, Is.EqualTo(0));
            Assert.That(result.NextQuestion, Is.Not.Null);
            Assert.That(result.IsRoundComplete, Is.False);
        }

        [Test]
        public async Task SubmitTriviaAnswerAsync_WrongAnswerMovesToNextQuestionWithoutRevealingAnswer()
        {
            var round = await _service.GetOrStartTriviaRoundAsync("user-1", new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc));
            var wrongOption = GetWrongAnswer(round.CurrentQuestion);

            var result = await _service.SubmitTriviaAnswerAsync(
                "user-1",
                new TriviaAnswerSubmission
                {
                    QuestionId = round.CurrentQuestion.QuestionId,
                    SelectedOptionKey = wrongOption
                },
                new DateTime(2026, 4, 28, 10, 1, 0, DateTimeKind.Utc));

            Assert.That(result.WasCorrect, Is.False);
            Assert.That(result.CorrectAnswers, Is.EqualTo(0));
            Assert.That(result.AwardedPoints, Is.EqualTo(0));
            Assert.That(result.NextQuestion, Is.Not.Null);
        }

        [Test]
        public async Task SubmitTriviaAnswerAsync_AwardsFivePointsAfterTenCorrectAnswers()
        {
            var date = new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc);
            TriviaAnswerResult result = null!;

            for (var i = 0; i < 10; i++)
            {
                var round = await _service.GetOrStartTriviaRoundAsync("user-1", date.AddMinutes(i));
                result = await _service.SubmitTriviaAnswerAsync(
                    "user-1",
                    new TriviaAnswerSubmission
                    {
                        QuestionId = round.CurrentQuestion.QuestionId,
                        SelectedOptionKey = round.CurrentQuestion.CorrectAnswerKey
                    },
                    date.AddMinutes(i));
            }

            Assert.That(result.WasCorrect, Is.True);
            Assert.That(result.CorrectAnswers, Is.EqualTo(10));
            Assert.That(result.IsRoundComplete, Is.True);
            Assert.That(result.AwardedPoints, Is.EqualTo(5));
            Assert.That(result.HasReachedDailyLimit, Is.True);

            var points = await _db.UserPoints.SingleAsync(x => x.UserId == "user-1");
            Assert.That(points.CurrentPoints, Is.EqualTo(5));
            Assert.That(points.LifetimePoints, Is.EqualTo(5));
        }

        [Test]
        public void SubmitTriviaAnswerAsync_InvalidQuestionIsRejected()
        {
            Assert.That(
                async () => await _service.SubmitTriviaAnswerAsync(
                    "user-1",
                    new TriviaAnswerSubmission
                    {
                        QuestionId = "bad-question",
                        SelectedOptionKey = "bad-answer"
                    },
                    new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc)),
                Throws.ArgumentException);
        }

        [Test]
        public void CompleteGameAsync_InvalidGameKeyIsRejected()
        {
            Assert.That(
                async () => await _service.CompleteGameAsync("user-1", "invalid-game", new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc)),
                Throws.ArgumentException);
        }

        [Test]
        public async Task CompleteGameAsync_PointsAreAddedToCurrentAndLifetimeTotals()
        {
            await _service.CompleteGameAsync("user-1", MinigameConstants.MatchingGameKey, new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc));

            var points = await _db.UserPoints.SingleAsync(x => x.UserId == "user-1");
            Assert.That(points.CurrentPoints, Is.EqualTo(1));
            Assert.That(points.LifetimePoints, Is.EqualTo(1));
        }

        [Test]
        public async Task CompleteGameAsync_MatchingStopsAwardingAfterFiveBoardClearsInOneDay()
        {
            var date = new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc);

            MinigameAwardResult result = null!;
            for (var i = 0; i < 6; i++)
            {
                result = await _service.CompleteGameAsync("user-1", MinigameConstants.MatchingGameKey, date.AddMinutes(i));
            }

            Assert.That(result.AwardedPoints, Is.EqualTo(0));
            Assert.That(result.DailyPointsEarned, Is.EqualTo(5));
            Assert.That(result.HasReachedDailyLimit, Is.True);

            var points = await _db.UserPoints.SingleAsync(x => x.UserId == "user-1");
            Assert.That(points.CurrentPoints, Is.EqualTo(5));
            Assert.That(points.LifetimePoints, Is.EqualTo(5));
        }

        [Test]
        public async Task CompleteGameAsync_TapRepairAwardsOnePointPerRound()
        {
            var result = await _service.CompleteGameAsync("user-1", MinigameConstants.TapRepairGameKey, new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc));

            Assert.That(result.AwardedPoints, Is.EqualTo(1));
            Assert.That(result.DailyPointsEarned, Is.EqualTo(1));
            Assert.That(result.HasReachedDailyLimit, Is.False);
        }

        [Test]
        public async Task CompleteGameAsync_TapRepairStopsAwardingAfterFiveRoundsInOneDay()
        {
            var date = new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc);

            MinigameAwardResult result = null!;
            for (var i = 0; i < 6; i++)
            {
                result = await _service.CompleteGameAsync("user-1", MinigameConstants.TapRepairGameKey, date.AddMinutes(i));
            }

            Assert.That(result.AwardedPoints, Is.EqualTo(0));
            Assert.That(result.DailyPointsEarned, Is.EqualTo(5));
            Assert.That(result.HasReachedDailyLimit, Is.True);

            var points = await _db.UserPoints.SingleAsync(x => x.UserId == "user-1");
            Assert.That(points.CurrentPoints, Is.EqualTo(5));
            Assert.That(points.LifetimePoints, Is.EqualTo(5));
        }

        [Test]
        public async Task CompleteGameAsync_TapRepairCalledTwiceQuicklyAwardsTwoPointsIntoOneDailyRecord()
        {
            var date = new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc);

            await _service.CompleteGameAsync("user-1", MinigameConstants.TapRepairGameKey, date);
            await _service.CompleteGameAsync("user-1", MinigameConstants.TapRepairGameKey, date);

            Assert.That(await _db.MinigamePlays.CountAsync(), Is.EqualTo(1));
            Assert.That(await _db.UserPoints.Select(x => x.CurrentPoints).SingleAsync(), Is.EqualTo(2));
            Assert.That(await _db.MinigamePlays.Select(x => x.PointsAwarded).SingleAsync(), Is.EqualTo(2));
        }

        [Test]
        public async Task SpinSlotsAsync_NonWinningSpinAwardsNoPoints()
        {
            var service = new MinigameService(_db, _httpContextAccessor, () => new[] { "cone", "bridge", "pothole" });
            var result = await service.SpinSlotsAsync("user-1", new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc));

            Assert.That(result.Symbols.Count, Is.EqualTo(3));
            Assert.That(result.Symbols.All(symbol => MinigameConstants.SlotSymbols.Contains(symbol)), Is.True);
            Assert.That(result.IsWinningSpin, Is.False);
            Assert.That(result.AwardedPoints, Is.EqualTo(0));
            Assert.That(await _db.UserPoints.AnyAsync(), Is.False);
        }

        [Test]
        public async Task SpinSlotsAsync_WinningSpinAwardsOnePoint()
        {
            var service = new MinigameService(_db, _httpContextAccessor, () => new[] { "cone", "cone", "cone" });
            var result = await service.SpinSlotsAsync("user-1", new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc));

            Assert.That(result.IsWinningSpin, Is.True);
            Assert.That(result.AwardedPoints, Is.EqualTo(1));
            Assert.That(result.DailyPointsEarned, Is.EqualTo(1));

            var points = await _db.UserPoints.SingleAsync(x => x.UserId == "user-1");
            Assert.That(points.CurrentPoints, Is.EqualTo(1));
            Assert.That(points.LifetimePoints, Is.EqualTo(1));
        }

        [Test]
        public async Task SpinSlotsAsync_WinningSpinsCapAtFivePointsPerDay()
        {
            var service = new MinigameService(_db, _httpContextAccessor, () => new[] { "bridge", "bridge", "bridge" });
            var date = new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc);

            SlotsSpinResult result = null!;
            for (var i = 0; i < 6; i++)
            {
                result = await service.SpinSlotsAsync("user-1", date.AddMinutes(i));
            }

            Assert.That(result.DailyPointsEarned, Is.EqualTo(5));
            Assert.That(result.HasReachedDailyLimit, Is.True);
            Assert.That(result.AwardedPoints, Is.EqualTo(0));

            var points = await _db.UserPoints.SingleAsync(x => x.UserId == "user-1");
            Assert.That(points.CurrentPoints, Is.EqualTo(5));
            Assert.That(points.LifetimePoints, Is.EqualTo(5));
        }

        [Test]
        public async Task SpinSlotsAsync_WinningSpinAfterDailyCapAwardsNoAdditionalPoints()
        {
            var service = new MinigameService(_db, _httpContextAccessor, () => new[] { "traffic-light", "traffic-light", "traffic-light" });
            var date = new DateTime(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc);

            for (var i = 0; i < 5; i++)
            {
                await service.SpinSlotsAsync("user-1", date.AddMinutes(i));
            }

            var cappedResult = await service.SpinSlotsAsync("user-1", date.AddMinutes(6));

            Assert.That(cappedResult.AwardedPoints, Is.EqualTo(0));
            Assert.That(cappedResult.DailyPointsEarned, Is.EqualTo(5));
            Assert.That(cappedResult.HasReachedDailyLimit, Is.True);
        }

        private static string GetWrongAnswer(TriviaQuestion question)
        {
            if (question.QuestionType == TriviaQuestionTypes.Text)
            {
                return "wrong-answer";
            }

            return question.Options
                .Select(option => option.OptionKey)
                .First(optionKey => !optionKey.Equals(question.CorrectAnswerKey, StringComparison.OrdinalIgnoreCase));
        }
    }
}
