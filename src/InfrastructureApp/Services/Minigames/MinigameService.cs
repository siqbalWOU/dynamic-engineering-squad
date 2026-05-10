using System.Text.Json;
using InfrastructureApp.Data;
using InfrastructureApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace InfrastructureApp.Services.Minigames
{
    public class MinigameService : IMinigameService
    {
        private const string TriviaSessionKey = "Minigames.Trivia.Round";
        private const int TriviaCorrectAnswersToWin = 10;

        private static readonly IReadOnlyList<TriviaQuestion> TriviaQuestions =
            new[]
            {
                CreateRadioQuestion(
                    "longest-bridge-water",
                    "What is the longest bridge over water in the world?",
                    "danyang-kunshan",
                    ("golden-gate", "Golden Gate Bridge"),
                    ("lake-pontchartrain", "Lake Pontchartrain Causeway"),
                    ("danyang-kunshan", "Danyang-Kunshan Grand Bridge"),
                    ("akashi-kaikyo", "Akashi Kaikyo Bridge")),
                CreateRadioQuestion(
                    "p3-meaning",
                    "In major infrastructure delivery, what does the acronym P3 stand for?",
                    "public-private-partnership",
                    ("public-project-plan", "Public Project Plan"),
                    ("public-private-partnership", "Public-Private Partnership"),
                    ("planned-public-program", "Planned Public Program"),
                    ("private-project-partnership", "Private Project Partnership")),
                CreateRadioQuestion(
                    "uk-france-tunnel",
                    "What is the name of the underwater railway tunnel that connects the United Kingdom and France?",
                    "channel-tunnel",
                    ("seikan", "Seikan Tunnel"),
                    ("channel-tunnel", "Channel Tunnel"),
                    ("gotthard", "Gotthard Base Tunnel"),
                    ("hampton", "Hampton Roads Tunnel")),
                CreateDropdownQuestion(
                    "transcontinental-railroad",
                    "Which historic infrastructure project, completed in 1869, connected the eastern and western railroad networks of the United States?",
                    "transcontinental-railroad",
                    ("erie-canal", "Erie Canal"),
                    ("transcontinental-railroad", "Transcontinental Railroad"),
                    ("panama-canal", "Panama Canal"),
                    ("interstate-system", "Interstate Highway System")),
                CreateRadioQuestion(
                    "hoover-dam",
                    "What is the name of the hydroelectric dam on the border between Arizona and Nevada?",
                    "hoover-dam",
                    ("glen-canyon", "Glen Canyon Dam"),
                    ("hoover-dam", "Hoover Dam"),
                    ("grand-coulee", "Grand Coulee Dam"),
                    ("three-gorges", "Three Gorges Dam")),
                CreateDropdownQuestion(
                    "ac-dc",
                    "On the power grid, what do AC and DC stand for?",
                    "alternating-direct",
                    ("alternating-direct", "Alternating Current and Direct Current"),
                    ("automatic-distributed", "Automatic Current and Distributed Current"),
                    ("amplified-driven", "Amplified Current and Driven Current"),
                    ("axial-digital", "Axial Current and Digital Current")),
                CreateRadioQuestion(
                    "snowy-hydro",
                    "What is Snowy Hydro 2.0 in Australia?",
                    "pumped-hydro",
                    ("coal-plant", "A new coal-fired power station"),
                    ("pumped-hydro", "A pumped-hydro renewable energy expansion project"),
                    ("desalination", "A coastal desalination facility"),
                    ("solar-road", "A solar highway pilot project")),
                CreateTrueFalseQuestion(
                    "nuclear-uranium",
                    "Nuclear power plants commonly rely on uranium for fission.",
                    true),
                CreateDropdownQuestion(
                    "aqueduct",
                    "Which Roman infrastructure innovation was used to transport fresh water into cities?",
                    "aqueduct",
                    ("colosseum", "The Colosseum"),
                    ("aqueduct", "Aqueduct"),
                    ("forum", "Forum"),
                    ("basilica", "Basilica")),
                CreateRadioQuestion(
                    "water-disinfection",
                    "Which chemical is widely used to disinfect drinking water?",
                    "chlorine",
                    ("nitrogen", "Nitrogen"),
                    ("chlorine", "Chlorine"),
                    ("helium", "Helium"),
                    ("argon", "Argon")),
                CreateDropdownQuestion(
                    "wastewater-system",
                    "What infrastructure network removes human waste to help prevent disease?",
                    "sewage-system",
                    ("sewage-system", "The sewage or wastewater system"),
                    ("storm-signals", "The storm signal system"),
                    ("district-heating", "The district heating system"),
                    ("freight-rail", "The freight rail system")),
                CreateRadioQuestion(
                    "fiber-cables",
                    "What cables made of glass or plastic transmit data using light?",
                    "fiber-optic",
                    ("copper-coax", "Copper coaxial cables"),
                    ("fiber-optic", "Fiber-optic cables"),
                    ("steel-armored", "Steel armored cables"),
                    ("twisted-pair", "Twisted-pair cables")),
                CreateTrueFalseQuestion(
                    "submarine-cables",
                    "Most intercontinental fiber-optic cables are routed along the ocean floor.",
                    true),
                CreateDropdownQuestion(
                    "belt-road",
                    "Which country proposed the Belt and Road Initiative?",
                    "china",
                    ("japan", "Japan"),
                    ("china", "China"),
                    ("india", "India"),
                    ("united-states", "United States")),
                CreateRadioQuestion(
                    "tallest-building",
                    "As of 2026, which building is the tallest man-made structure and skyscraper in the world?",
                    "burj-khalifa",
                    ("merdeka-118", "Merdeka 118"),
                    ("shanghai-tower", "Shanghai Tower"),
                    ("burj-khalifa", "Burj Khalifa"),
                    ("taipei-101", "Taipei 101")),
                CreateDropdownQuestion(
                    "concrete",
                    "What is the most widely used construction material made from water, aggregates, and cement?",
                    "concrete",
                    ("asphalt", "Asphalt"),
                    ("glass", "Glass"),
                    ("concrete", "Concrete"),
                    ("brick", "Brick")),
                CreateRadioQuestion(
                    "panama-canal",
                    "Which canal links the Atlantic and Pacific Oceans?",
                    "panama",
                    ("erie", "Erie Canal"),
                    ("suez", "Suez Canal"),
                    ("panama", "Panama Canal"),
                    ("corinth", "Corinth Canal")),
                CreateRadioQuestion(
                    "suez-canal",
                    "The Suez Canal connects which two bodies of water?",
                    "mediterranean-red-sea",
                    ("atlantic-pacific", "Atlantic Ocean and Pacific Ocean"),
                    ("mediterranean-red-sea", "Mediterranean Sea and Red Sea"),
                    ("black-caspian", "Black Sea and Caspian Sea"),
                    ("north-baltic", "North Sea and Baltic Sea")),
                CreateTrueFalseQuestion(
                    "substation-voltage",
                    "An electrical substation can change voltage levels so power can be transmitted efficiently.",
                    true),
                CreateDropdownQuestion(
                    "tbm-meaning",
                    "In tunneling, what does TBM stand for?",
                    "tunnel-boring-machine",
                    ("track-building-machine", "Track Building Machine"),
                    ("tunnel-boring-machine", "Tunnel Boring Machine"),
                    ("transport-barrier-module", "Transport Barrier Module"),
                    ("terrain-balance-meter", "Terrain Balance Meter")),
                CreateTrueFalseQuestion(
                    "levees",
                    "Levees are built to reduce flood risk from rivers or storm surge.",
                    true),
                CreateRadioQuestion(
                    "golden-gate-type",
                    "What type of bridge is the Golden Gate Bridge?",
                    "suspension",
                    ("arch", "Arch bridge"),
                    ("beam", "Beam bridge"),
                    ("suspension", "Suspension bridge"),
                    ("cantilever", "Cantilever bridge")),
                CreateDropdownQuestion(
                    "interstate-year",
                    "In what year was the Federal-Aid Highway Act signed, launching the Interstate Highway System?",
                    "1956",
                    ("1945", "1945"),
                    ("1956", "1956"),
                    ("1965", "1965"),
                    ("1972", "1972")),
                CreateRadioQuestion(
                    "led-meaning",
                    "In traffic signals and street lighting, what does LED stand for?",
                    "light-emitting-diode",
                    ("low-energy-device", "Low Energy Device"),
                    ("light-emitting-diode", "Light-Emitting Diode"),
                    ("linear-electric-display", "Linear Electric Display"),
                    ("light-energy-driver", "Light Energy Driver")),
                CreateDropdownQuestion(
                    "power-unit",
                    "Which unit measures electric power?",
                    "watt",
                    ("volt", "Volt"),
                    ("ohm", "Ohm"),
                    ("watt", "Watt"),
                    ("ampere-hour", "Ampere-hour")),
                CreateTextQuestion(
                    "square-area",
                    "A square maintenance yard is 12 meters on each side. Enter its area in square meters as a number only.",
                    "144",
                    acceptedTextAnswers: new[] { "144", "144.0", "144.00" },
                    textPlaceholder: "Example: 144"),
                CreateTextQuestion(
                    "circle-area",
                    "A circular pothole has a radius of 3 meters. Using pi = 3.14, enter the area rounded to two decimals.",
                    "28.26",
                    acceptedTextAnswers: new[] { "28.26" },
                    textPlaceholder: "Example: 28.26"),
                CreateTrueFalseQuestion(
                    "asphalt-flexible",
                    "Asphalt pavement is generally considered a flexible pavement system.",
                    true),
                CreateRadioQuestion(
                    "storm-drains",
                    "What is the main purpose of storm drains?",
                    "remove-runoff",
                    ("carry-sewage", "Carry household sewage to treatment plants"),
                    ("store-drinking-water", "Store drinking water underground"),
                    ("remove-runoff", "Remove rainwater runoff from streets"),
                    ("generate-power", "Generate electricity from rain")),
                CreateDropdownQuestion(
                    "roundabout-benefit",
                    "Which safety benefit is commonly associated with modern roundabouts?",
                    "lower-severe-crashes",
                    ("higher-speeds", "Higher approach speeds"),
                    ("lower-severe-crashes", "Fewer severe angle crashes"),
                    ("more-lane-closures", "More lane closure needs"),
                    ("longer-signals", "Longer traffic signal phases")),
                CreateTextQuestion(
                    "rectangle-area",
                    "A repair zone is 8 meters wide and 15 meters long. Enter its area in square meters as a number only.",
                    "120",
                    acceptedTextAnswers: new[] { "120", "120.0", "120.00" },
                    textPlaceholder: "Example: 120")
            };

        private readonly ApplicationDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Func<IReadOnlyList<string>> _slotSymbolProvider;

        public MinigameService(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
            : this(db, httpContextAccessor, CreateDefaultSlotSymbolProvider())
        {
        }

        public MinigameService(
            ApplicationDbContext db,
            IHttpContextAccessor httpContextAccessor,
            Func<IReadOnlyList<string>> slotSymbolProvider)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _slotSymbolProvider = slotSymbolProvider;
        }

        public async Task<IReadOnlyList<MinigameStatus>> GetTodayStatusesAsync(string userId, DateTime? utcNow = null)
        {
            var today = GetPlayedOnDate(utcNow);

            var todayPlays = await _db.MinigamePlays
                .Where(play => play.UserId == userId && play.PlayedOnDate == today)
                .ToListAsync();

            return MinigameConstants.SupportedGameKeys
                .Select(gameKey => new MinigameStatus
                {
                    GameKey = gameKey,
                    DailyPointsEarned = todayPlays
                        .Where(play => play.GameKey.Equals(gameKey, StringComparison.OrdinalIgnoreCase))
                        .Select(play => play.PointsAwarded)
                        .FirstOrDefault(),
                    DailyPointsLimit = MinigameConstants.PointsPerGame,
                    HasReachedDailyLimit = todayPlays
                        .Where(play => play.GameKey.Equals(gameKey, StringComparison.OrdinalIgnoreCase))
                        .Select(play => play.PointsAwarded >= MinigameConstants.PointsPerGame)
                        .FirstOrDefault()
                })
                .ToList();
        }

        public async Task<int> GetCurrentPointsAsync(string userId)
        {
            return await _db.UserPoints
                .Where(points => points.UserId == userId)
                .Select(points => (int?)points.CurrentPoints)
                .FirstOrDefaultAsync() ?? 0;
        }

        public Task<MinigameAwardResult> CompleteGameAsync(string userId, string gameKey, DateTime? utcNow = null)
        {
            if (string.Equals(gameKey, MinigameConstants.MatchingGameKey, StringComparison.OrdinalIgnoreCase)
                || string.Equals(gameKey, MinigameConstants.TapRepairGameKey, StringComparison.OrdinalIgnoreCase))
            {
                return AwardRepeatableGamePointAsync(userId, gameKey.Trim().ToLowerInvariant(), utcNow);
            }

            return CompleteGameInternalAsync(userId, gameKey, utcNow);
        }

        public async Task<SlotsSpinResult> SpinSlotsAsync(string userId, DateTime? utcNow = null)
        {
            var symbols = _slotSymbolProvider().ToArray();
            var isWinningSpin = symbols.Length == 3 && symbols.Distinct(StringComparer.Ordinal).Count() == 1;
            var awardResult = await AwardSlotsSpinAsync(userId, isWinningSpin, utcNow);

            return new SlotsSpinResult
            {
                GameKey = awardResult.GameKey,
                AwardedPoints = awardResult.AwardedPoints,
                CurrentPoints = awardResult.CurrentPoints,
                PlayedOnDate = awardResult.PlayedOnDate,
                DailyPointsEarned = awardResult.DailyPointsEarned,
                DailyPointsLimit = awardResult.DailyPointsLimit,
                HasReachedDailyLimit = awardResult.HasReachedDailyLimit,
                Symbols = symbols,
                IsWinningSpin = isWinningSpin,
                ResultLabel = isWinningSpin ? "Three of a Kind" : "No Match"
            };
        }

        public async Task<TriviaQuestionPromptResult> GetOrStartTriviaRoundAsync(string userId, DateTime? utcNow = null)
        {
            var dailyStatus = await GetStatusAsync(userId, MinigameConstants.TriviaGameKey, utcNow);
            if (dailyStatus.HasReachedDailyLimit)
            {
                ClearTriviaRound();
                return new TriviaQuestionPromptResult
                {
                    CorrectAnswers = TriviaCorrectAnswersToWin,
                    CorrectAnswersToWin = TriviaCorrectAnswersToWin,
                    CurrentPoints = await GetCurrentPointsAsync(userId),
                    DailyPointsEarned = dailyStatus.DailyPointsEarned,
                    DailyPointsLimit = dailyStatus.DailyPointsLimit,
                    HasReachedDailyLimit = true,
                    IsRoundComplete = true
                };
            }

            var state = GetTriviaRoundFromSession();
            if (state == null || state.IsComplete || !string.Equals(state.UserId, userId, StringComparison.Ordinal))
            {
                state = CreateNewTriviaRound(userId);
                SaveTriviaRound(state);
            }

            var currentQuestion = GetQuestionById(state.CurrentQuestionId);

            return new TriviaQuestionPromptResult
            {
                CurrentQuestion = currentQuestion,
                CorrectAnswers = state.CorrectAnswers,
                CorrectAnswersToWin = TriviaCorrectAnswersToWin,
                CurrentPoints = await GetCurrentPointsAsync(userId),
                DailyPointsEarned = dailyStatus.DailyPointsEarned,
                DailyPointsLimit = dailyStatus.DailyPointsLimit,
                HasReachedDailyLimit = dailyStatus.HasReachedDailyLimit,
                IsRoundComplete = state.IsComplete
            };
        }

        public async Task<TriviaAnswerResult> SubmitTriviaAnswerAsync(string userId, TriviaAnswerSubmission answer, DateTime? utcNow = null)
        {
            if (answer == null || string.IsNullOrWhiteSpace(answer.QuestionId) || string.IsNullOrWhiteSpace(answer.SelectedOptionKey))
            {
                throw new ArgumentException("Trivia answer is incomplete.", nameof(answer));
            }

            var state = GetTriviaRoundFromSession();
            if (state == null || !string.Equals(state.UserId, userId, StringComparison.Ordinal) || state.IsComplete)
            {
                throw new ArgumentException("Trivia round is not active.", nameof(answer));
            }

            if (!string.Equals(state.CurrentQuestionId, answer.QuestionId, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Trivia answer does not match the current question.", nameof(answer));
            }

            var question = GetQuestionById(state.CurrentQuestionId);
            var wasCorrect = ValidateTriviaAnswer(question, answer.SelectedOptionKey);

            state.QuestionsAnswered += 1;
            if (wasCorrect)
            {
                state.CorrectAnswers += 1;
            }

            var awardedPoints = 0;
            TriviaQuestion? nextQuestion = null;

            if (state.CorrectAnswers >= TriviaCorrectAnswersToWin)
            {
                state.IsComplete = true;
                SaveTriviaRound(state);

                var awardResult = await CompleteGameInternalAsync(userId, MinigameConstants.TriviaGameKey, utcNow);
                awardedPoints = awardResult.AwardedPoints;

                return new TriviaAnswerResult
                {
                    WasCorrect = true,
                    CorrectAnswers = state.CorrectAnswers,
                    CorrectAnswersToWin = TriviaCorrectAnswersToWin,
                    IsRoundComplete = true,
                    AwardedPoints = awardedPoints,
                    CurrentPoints = awardResult.CurrentPoints,
                    DailyPointsEarned = awardResult.DailyPointsEarned,
                    DailyPointsLimit = awardResult.DailyPointsLimit,
                    HasReachedDailyLimit = awardResult.HasReachedDailyLimit,
                    ResultMessage = awardedPoints > 0
                        ? "Correct. You reached 10 correct answers and earned 5 points."
                        : "Correct. You reached 10 correct answers, but today's trivia reward was already claimed."
                };
            }

            MoveToNextTriviaQuestion(state);
            SaveTriviaRound(state);
            nextQuestion = GetQuestionById(state.CurrentQuestionId);

            var dailyStatus = await GetStatusAsync(userId, MinigameConstants.TriviaGameKey, utcNow);

            return new TriviaAnswerResult
            {
                WasCorrect = wasCorrect,
                CorrectAnswers = state.CorrectAnswers,
                CorrectAnswersToWin = TriviaCorrectAnswersToWin,
                IsRoundComplete = false,
                AwardedPoints = 0,
                CurrentPoints = await GetCurrentPointsAsync(userId),
                DailyPointsEarned = dailyStatus.DailyPointsEarned,
                DailyPointsLimit = dailyStatus.DailyPointsLimit,
                HasReachedDailyLimit = dailyStatus.HasReachedDailyLimit,
                NextQuestion = nextQuestion,
                ResultMessage = wasCorrect
                    ? "Correct. Moving to the next question."
                    : "Incorrect. Moving to the next question."
            };
        }

        private async Task<MinigameAwardResult> CompleteGameInternalAsync(string userId, string gameKey, DateTime? utcNow)
        {
            if (!MinigameConstants.IsSupportedGameKey(gameKey))
            {
                throw new ArgumentException("Invalid minigame key.", nameof(gameKey));
            }

            var normalizedGameKey = gameKey.Trim().ToLowerInvariant();
            var today = GetPlayedOnDate(utcNow);

            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var existingPlay = await _db.MinigamePlays
                    .FirstOrDefaultAsync(play =>
                        play.UserId == userId &&
                        play.GameKey == normalizedGameKey &&
                        play.PlayedOnDate == today);

                if (existingPlay != null)
                {
                    await tx.CommitAsync();
                    return new MinigameAwardResult
                    {
                        GameKey = normalizedGameKey,
                        AwardedPoints = 0,
                        CurrentPoints = await GetCurrentPointsAsync(userId),
                        PlayedOnDate = today,
                        DailyPointsEarned = existingPlay.PointsAwarded,
                        DailyPointsLimit = MinigameConstants.PointsPerGame,
                        HasReachedDailyLimit = true
                    };
                }

                var userPoints = await GetOrCreateUserPointsAsync(userId);

                _db.MinigamePlays.Add(new MinigamePlay
                {
                    UserId = userId,
                    GameKey = normalizedGameKey,
                    PlayedOnDate = today,
                    PointsAwarded = MinigameConstants.PointsPerGame,
                    CreatedAt = DateTime.UtcNow
                });

                userPoints.CurrentPoints += MinigameConstants.PointsPerGame;
                userPoints.LifetimePoints += MinigameConstants.PointsPerGame;
                userPoints.LastUpdated = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return new MinigameAwardResult
                {
                    GameKey = normalizedGameKey,
                    AwardedPoints = MinigameConstants.PointsPerGame,
                    CurrentPoints = userPoints.CurrentPoints,
                    PlayedOnDate = today,
                    DailyPointsEarned = MinigameConstants.PointsPerGame,
                    DailyPointsLimit = MinigameConstants.PointsPerGame,
                    HasReachedDailyLimit = true
                };
            }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync();
                _db.ChangeTracker.Clear();

                if (await WasAlreadyPlayedTodayAsync(userId, normalizedGameKey, today))
                {
                    return new MinigameAwardResult
                    {
                        GameKey = normalizedGameKey,
                        AwardedPoints = 0,
                        CurrentPoints = await GetCurrentPointsAsync(userId),
                        PlayedOnDate = today,
                        DailyPointsEarned = MinigameConstants.PointsPerGame,
                        DailyPointsLimit = MinigameConstants.PointsPerGame,
                        HasReachedDailyLimit = true
                    };
                }

                throw;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private async Task<bool> WasAlreadyPlayedTodayAsync(string userId, string gameKey, DateTime today)
        {
            return await _db.MinigamePlays.AnyAsync(play =>
                play.UserId == userId &&
                play.GameKey == gameKey &&
                play.PlayedOnDate == today);
        }

        private static DateTime GetPlayedOnDate(DateTime? utcNow)
        {
            var now = utcNow ?? DateTime.UtcNow;
            if (now.Kind == DateTimeKind.Unspecified)
            {
                now = DateTime.SpecifyKind(now, DateTimeKind.Utc);
            }

            var localNow = now.Kind == DateTimeKind.Local
                ? now
                : TimeZoneInfo.ConvertTimeFromUtc(now, TimeZoneInfo.Local);

            return localNow.Date;
        }

        private async Task<MinigameAwardResult> AwardSlotsSpinAsync(string userId, bool isWinningSpin, DateTime? utcNow)
        {
            var today = GetPlayedOnDate(utcNow);

            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var existingPlay = await _db.MinigamePlays
                    .FirstOrDefaultAsync(play =>
                        play.UserId == userId &&
                        play.GameKey == MinigameConstants.SlotsGameKey &&
                        play.PlayedOnDate == today);

                var dailyPointsEarned = existingPlay?.PointsAwarded ?? 0;
                var hasReachedDailyLimit = dailyPointsEarned >= MinigameConstants.PointsPerGame;

                if (!isWinningSpin || hasReachedDailyLimit)
                {
                    await tx.CommitAsync();
                    return new MinigameAwardResult
                    {
                        GameKey = MinigameConstants.SlotsGameKey,
                        AwardedPoints = 0,
                        CurrentPoints = await GetCurrentPointsAsync(userId),
                        PlayedOnDate = today,
                        DailyPointsEarned = dailyPointsEarned,
                        DailyPointsLimit = MinigameConstants.PointsPerGame,
                        HasReachedDailyLimit = hasReachedDailyLimit
                    };
                }

                var userPoints = await GetOrCreateUserPointsAsync(userId);

                if (existingPlay == null)
                {
                    existingPlay = new MinigamePlay
                    {
                        UserId = userId,
                        GameKey = MinigameConstants.SlotsGameKey,
                        PlayedOnDate = today,
                        PointsAwarded = 0,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.MinigamePlays.Add(existingPlay);
                }

                existingPlay.PointsAwarded += 1;
                userPoints.CurrentPoints += 1;
                userPoints.LifetimePoints += 1;
                userPoints.LastUpdated = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return new MinigameAwardResult
                {
                    GameKey = MinigameConstants.SlotsGameKey,
                    AwardedPoints = 1,
                    CurrentPoints = userPoints.CurrentPoints,
                    PlayedOnDate = today,
                    DailyPointsEarned = existingPlay.PointsAwarded,
                    DailyPointsLimit = MinigameConstants.PointsPerGame,
                    HasReachedDailyLimit = existingPlay.PointsAwarded >= MinigameConstants.PointsPerGame
                };
            }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync();
                _db.ChangeTracker.Clear();
                throw;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private async Task<MinigameAwardResult> AwardRepeatableGamePointAsync(string userId, string gameKey, DateTime? utcNow)
        {
            var today = GetPlayedOnDate(utcNow);

            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var existingPlay = await _db.MinigamePlays
                    .FirstOrDefaultAsync(play =>
                        play.UserId == userId &&
                        play.GameKey == gameKey &&
                        play.PlayedOnDate == today);

                var dailyPointsEarned = existingPlay?.PointsAwarded ?? 0;
                var hasReachedDailyLimit = dailyPointsEarned >= MinigameConstants.PointsPerGame;

                if (hasReachedDailyLimit)
                {
                    await tx.CommitAsync();
                    return new MinigameAwardResult
                    {
                        GameKey = gameKey,
                        AwardedPoints = 0,
                        CurrentPoints = await GetCurrentPointsAsync(userId),
                        PlayedOnDate = today,
                        DailyPointsEarned = dailyPointsEarned,
                        DailyPointsLimit = MinigameConstants.PointsPerGame,
                        HasReachedDailyLimit = true
                    };
                }

                var userPoints = await GetOrCreateUserPointsAsync(userId);

                if (existingPlay == null)
                {
                    existingPlay = new MinigamePlay
                    {
                        UserId = userId,
                        GameKey = gameKey,
                        PlayedOnDate = today,
                        PointsAwarded = 0,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.MinigamePlays.Add(existingPlay);
                }

                existingPlay.PointsAwarded += 1;
                userPoints.CurrentPoints += 1;
                userPoints.LifetimePoints += 1;
                userPoints.LastUpdated = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return new MinigameAwardResult
                {
                    GameKey = gameKey,
                    AwardedPoints = 1,
                    CurrentPoints = userPoints.CurrentPoints,
                    PlayedOnDate = today,
                    DailyPointsEarned = existingPlay.PointsAwarded,
                    DailyPointsLimit = MinigameConstants.PointsPerGame,
                    HasReachedDailyLimit = existingPlay.PointsAwarded >= MinigameConstants.PointsPerGame
                };
            }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync();
                _db.ChangeTracker.Clear();
                throw;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private static Func<IReadOnlyList<string>> CreateDefaultSlotSymbolProvider()
        {
            var random = new Random();

            return () => Enumerable.Range(0, 3)
                .Select(_ => MinigameConstants.SlotSymbols[random.Next(MinigameConstants.SlotSymbols.Length)])
                .ToArray();
        }

        private async Task<UserPoints> GetOrCreateUserPointsAsync(string userId)
        {
            var userPoints = await _db.UserPoints.FirstOrDefaultAsync(points => points.UserId == userId);
            if (userPoints != null)
            {
                return userPoints;
            }

            userPoints = new UserPoints
            {
                UserId = userId,
                CurrentPoints = 0,
                LifetimePoints = 0,
                LastUpdated = DateTime.UtcNow
            };

            _db.UserPoints.Add(userPoints);
            return userPoints;
        }

        private async Task<MinigameStatus> GetStatusAsync(string userId, string gameKey, DateTime? utcNow)
        {
            return (await GetTodayStatusesAsync(userId, utcNow))
                .FirstOrDefault(status => status.GameKey == gameKey)
                ?? new MinigameStatus
                {
                    GameKey = gameKey,
                    DailyPointsLimit = MinigameConstants.PointsPerGame
                };
        }

        private TriviaRoundState CreateNewTriviaRound(string userId)
        {
            var remainingQuestionIds = ShuffleQuestionIds().ToList();
            var currentQuestionId = remainingQuestionIds[0];
            remainingQuestionIds.RemoveAt(0);

            return new TriviaRoundState
            {
                UserId = userId,
                CurrentQuestionId = currentQuestionId,
                RemainingQuestionIds = remainingQuestionIds
            };
        }

        private void MoveToNextTriviaQuestion(TriviaRoundState state)
        {
            if (state.RemainingQuestionIds.Count == 0)
            {
                state.RemainingQuestionIds = ShuffleQuestionIds()
                    .Where(questionId => !string.Equals(questionId, state.CurrentQuestionId, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            state.CurrentQuestionId = state.RemainingQuestionIds[0];
            state.RemainingQuestionIds.RemoveAt(0);
        }

        private IEnumerable<string> ShuffleQuestionIds()
        {
            return TriviaQuestions
                .Select(question => question.QuestionId)
                .OrderBy(_ => Random.Shared.Next());
        }

        private TriviaQuestion GetQuestionById(string questionId)
        {
            return TriviaQuestions.FirstOrDefault(question =>
                       question.QuestionId.Equals(questionId, StringComparison.OrdinalIgnoreCase))
                   ?? throw new ArgumentException("Unknown trivia question.", nameof(questionId));
        }

        private static bool ValidateTriviaAnswer(TriviaQuestion question, string submittedAnswer)
        {
            if (question.QuestionType == TriviaQuestionTypes.Text)
            {
                var normalizedAnswer = NormalizeTextAnswer(submittedAnswer);
                return question.AcceptedTextAnswers
                    .Select(NormalizeTextAnswer)
                    .Any(answer => answer == normalizedAnswer);
            }

            return question.Options.Any(option => option.OptionKey.Equals(submittedAnswer, StringComparison.OrdinalIgnoreCase))
                   && question.CorrectAnswerKey.Equals(submittedAnswer, StringComparison.OrdinalIgnoreCase);
        }

        private TriviaRoundState? GetTriviaRoundFromSession()
        {
            var session = GetSession();
            var rawState = session.GetString(TriviaSessionKey);
            if (string.IsNullOrWhiteSpace(rawState))
            {
                return null;
            }

            return JsonSerializer.Deserialize<TriviaRoundState>(rawState);
        }

        private void SaveTriviaRound(TriviaRoundState state)
        {
            var session = GetSession();
            session.SetString(TriviaSessionKey, JsonSerializer.Serialize(state));
        }

        private void ClearTriviaRound()
        {
            GetSession().Remove(TriviaSessionKey);
        }

        private ISession GetSession()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            if (session == null)
            {
                throw new InvalidOperationException("Trivia requires an active HTTP session.");
            }

            return session;
        }

        private static string NormalizeTextAnswer(string answer)
        {
            return answer.Trim().ToLowerInvariant();
        }

        private static TriviaQuestion CreateRadioQuestion(
            string questionId,
            string prompt,
            string correctAnswerKey,
            params (string OptionKey, string Label)[] options)
        {
            return CreateChoiceQuestion(TriviaQuestionTypes.Radio, questionId, prompt, correctAnswerKey, options);
        }

        private static TriviaQuestion CreateDropdownQuestion(
            string questionId,
            string prompt,
            string correctAnswerKey,
            params (string OptionKey, string Label)[] options)
        {
            return CreateChoiceQuestion(TriviaQuestionTypes.Dropdown, questionId, prompt, correctAnswerKey, options);
        }

        private static TriviaQuestion CreateTrueFalseQuestion(string questionId, string prompt, bool correctAnswer)
        {
            return new TriviaQuestion
            {
                QuestionId = questionId,
                Prompt = prompt,
                QuestionType = TriviaQuestionTypes.TrueFalse,
                CorrectAnswerKey = correctAnswer ? "true" : "false",
                Options = new[]
                {
                    new TriviaOption { OptionKey = "true", Label = "True" },
                    new TriviaOption { OptionKey = "false", Label = "False" }
                }
            };
        }

        private static TriviaQuestion CreateTextQuestion(
            string questionId,
            string prompt,
            string correctAnswerKey,
            IReadOnlyList<string> acceptedTextAnswers,
            string textPlaceholder)
        {
            return new TriviaQuestion
            {
                QuestionId = questionId,
                Prompt = prompt,
                QuestionType = TriviaQuestionTypes.Text,
                CorrectAnswerKey = correctAnswerKey,
                AcceptedTextAnswers = acceptedTextAnswers,
                TextPlaceholder = textPlaceholder
            };
        }

        private static TriviaQuestion CreateChoiceQuestion(
            string questionType,
            string questionId,
            string prompt,
            string correctAnswerKey,
            params (string OptionKey, string Label)[] options)
        {
            return new TriviaQuestion
            {
                QuestionId = questionId,
                Prompt = prompt,
                QuestionType = questionType,
                CorrectAnswerKey = correctAnswerKey,
                Options = options
                    .Select(option => new TriviaOption
                    {
                        OptionKey = option.OptionKey,
                        Label = option.Label
                    })
                    .ToArray()
            };
        }
    }
}
