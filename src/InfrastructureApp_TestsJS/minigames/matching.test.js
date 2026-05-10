const {
    setupDOM,
    loadScript,
    flushPromises,
    mockAudioController,
    setMockLocation
} = require("./testUtils");

describe("matching.js", () => {
    let audioController;

    function buildDOM() {
        setupDOM(`
            <input name="__RequestVerificationToken" value="token-456" />
            <div id="matchingResult" class="alert alert-secondary mb-3"></div>
            <div id="matchingBoard" data-complete-url="/Minigames/CompleteGame" data-game-key="matching"></div>
            <button id="matchingRestartButton" type="button">Shuffle Board</button>
            <button id="matchingMuteButton" type="button">Mute Music</button>
            <div id="matchingCurrentPoints">0</div>
            <div id="matchingDailyProgress">0 / 5</div>
        `);
    }

    function getCards() {
        return [...document.querySelectorAll(".matching-card")];
    }

    function getPairByKey(key) {
        return getCards().filter((card) => card.dataset.cardKey === key);
    }

    beforeEach(() => {
        jest.resetModules();
        jest.useFakeTimers();
        audioController = mockAudioController();
        window.createMinigameAudio = jest.fn(() => audioController);
        global.fetch = jest.fn();
        setMockLocation();
    });

    afterEach(() => {
        jest.clearAllMocks();
        jest.useRealTimers();
    });

    test("exits without throwing if required DOM elements are missing", () => {
        setupDOM(`<div></div>`);
        expect(() => loadScript("matching.js")).not.toThrow();
    });

    test("initializes board with 12 cards", () => {
        buildDOM();
        loadScript("matching.js");

        expect(getCards()).toHaveLength(12);
        const revealedImages = getCards().map((card) => card.querySelector(".matching-card-face-back img"));
        expect(revealedImages.every((image) => image instanceof HTMLImageElement)).toBe(true);
    });

    test("revealed cards use image assets with alt text", () => {
        buildDOM();
        loadScript("matching.js");

        const firstCardImage = getCards()[0].querySelector(".matching-card-face-back img");
        expect(firstCardImage).not.toBeNull();
        expect(firstCardImage.getAttribute("src")).toContain("/Images/minigames/symbols/");
        expect(firstCardImage.getAttribute("alt")).not.toBe("");
    });

    test("clicking a card flips it and clicking it again does nothing", () => {
        buildDOM();
        loadScript("matching.js");

        const card = getCards()[0];
        card.click();
        expect(card.classList.contains("is-flipped")).toBe(true);
        expect(audioController.playIfNeeded).toHaveBeenCalledTimes(1);

        card.click();
        expect(audioController.playIfNeeded).toHaveBeenCalledTimes(1);
    });

    test("matching pair becomes matched", () => {
        buildDOM();
        loadScript("matching.js");

        const pair = getPairByKey(getCards()[0].dataset.cardKey);
        pair[0].click();
        pair[1].click();

        expect(pair[0].classList.contains("is-matched")).toBe(true);
        expect(pair[1].classList.contains("is-matched")).toBe(true);
    });

    test("non-matching cards flip back after 700ms", () => {
        buildDOM();
        loadScript("matching.js");

        const cards = getCards();
        const first = cards[0];
        const second = cards.find((card) => card.dataset.cardKey !== first.dataset.cardKey);

        first.click();
        second.click();

        expect(first.classList.contains("is-flipped")).toBe(true);
        expect(second.classList.contains("is-flipped")).toBe(true);

        jest.advanceTimersByTime(700);

        expect(first.classList.contains("is-flipped")).toBe(false);
        expect(second.classList.contains("is-flipped")).toBe(false);
    });

    test("completing all pairs posts game completion and updates success state", async () => {
        buildDOM();
        global.fetch.mockResolvedValue({
            ok: true,
            json: async () => ({
                currentPoints: 12,
                dailyPointsEarned: 3,
                dailyPointsLimit: 5,
                awardedPoints: 1,
                hasReachedDailyLimit: false
            })
        });

        loadScript("matching.js");

        const uniqueKeys = [...new Set(getCards().map((card) => card.dataset.cardKey))];
        uniqueKeys.forEach((key) => {
            const pair = getPairByKey(key);
            pair[0].click();
            pair[1].click();
        });

        await flushPromises();

        expect(fetch).toHaveBeenCalledWith("/Minigames/CompleteGame", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                RequestVerificationToken: "token-456"
            },
            body: JSON.stringify({
                gameKey: "matching"
            })
        });
        expect(document.getElementById("matchingCurrentPoints").textContent).toBe("12");
        expect(document.getElementById("matchingDailyProgress").textContent).toBe("3 / 5");
        expect(document.getElementById("matchingResult").className).toContain("alert-success");
        expect(audioController.stop).not.toHaveBeenCalled();
    });

    test("awarded points 0 shows daily-limit warning", async () => {
        buildDOM();
        global.fetch.mockResolvedValue({
            ok: true,
            json: async () => ({
                currentPoints: 12,
                dailyPointsEarned: 5,
                dailyPointsLimit: 5,
                awardedPoints: 0,
                hasReachedDailyLimit: true
            })
        });

        loadScript("matching.js");
        const uniqueKeys = [...new Set(getCards().map((card) => card.dataset.cardKey))];
        uniqueKeys.forEach((key) => {
            const pair = getPairByKey(key);
            pair[0].click();
            pair[1].click();
        });
        await flushPromises();

        expect(document.getElementById("matchingResult").className).toContain("alert-warning");
        expect(audioController.stop).toHaveBeenCalledTimes(1);
    });

    test("failed completion shows danger message and allows retry", async () => {
        buildDOM();
        jest.spyOn(console, "error").mockImplementation(() => { });
        global.fetch.mockRejectedValue(new Error("network"));

        loadScript("matching.js");
        const uniqueKeys = [...new Set(getCards().map((card) => card.dataset.cardKey))];
        uniqueKeys.forEach((key) => {
            const pair = getPairByKey(key);
            pair[0].click();
            pair[1].click();
        });
        await flushPromises();

        expect(document.getElementById("matchingResult").className).toContain("alert-danger");

        const restartButton = document.getElementById("matchingRestartButton");
        restartButton.click();
        expect(getCards()).toHaveLength(12);
        expect(document.getElementById("matchingResult").textContent).toContain("Flip cards and find all matching pairs");
        expect(audioController.stop).not.toHaveBeenCalled();
    });

    test("401 redirects to login", async () => {
        buildDOM();
        const consoleErrorSpy = jest.spyOn(console, "error").mockImplementation(() => { });
        global.fetch.mockResolvedValue({ ok: false, status: 401 });

        loadScript("matching.js");
        const uniqueKeys = [...new Set(getCards().map((card) => card.dataset.cardKey))];
        uniqueKeys.forEach((key) => {
            const pair = getPairByKey(key);
            pair[0].click();
            pair[1].click();
        });
        await flushPromises();

        expect(consoleErrorSpy).toHaveBeenCalled();
        expect(audioController.stop).not.toHaveBeenCalled();
    });

    test("repeated card clicks do not stop music between turns or on restart", () => {
        buildDOM();
        loadScript("matching.js");

        const cards = getCards();
        cards[0].click();
        cards[1].click();
        document.getElementById("matchingRestartButton").click();

        expect(audioController.playIfNeeded).toHaveBeenCalledTimes(2);
        expect(audioController.stop).not.toHaveBeenCalled();
    });

    test("daily limit on page load prevents music from starting during practice play", () => {
        setupDOM(`
            <input name="__RequestVerificationToken" value="token-456" />
            <div id="matchingResult" class="alert alert-warning mb-3"></div>
            <div id="matchingBoard" data-complete-url="/Minigames/CompleteGame" data-game-key="matching"></div>
            <button id="matchingRestartButton" type="button">Shuffle Board</button>
            <button id="matchingMuteButton" type="button">Mute Music</button>
            <div id="matchingCurrentPoints">0</div>
            <div id="matchingDailyProgress">5 / 5</div>
        `);

        loadScript("matching.js");
        getCards()[0].click();

        expect(audioController.playIfNeeded).not.toHaveBeenCalled();
    });

    test("restart rebuilds board and mute button toggles text", () => {
        buildDOM();
        audioController.toggleMute.mockReturnValueOnce(true).mockReturnValueOnce(false);
        loadScript("matching.js");

        const originalCardIds = getCards().map((card) => card.dataset.cardId);
        document.getElementById("matchingRestartButton").click();
        const rebuiltCardIds = getCards().map((card) => card.dataset.cardId);
        expect(rebuiltCardIds).toHaveLength(12);
        expect(rebuiltCardIds.sort()).toEqual(originalCardIds.sort());

        const muteButton = document.getElementById("matchingMuteButton");
        muteButton.click();
        expect(muteButton.textContent).toBe("Unmute Music");
        muteButton.click();
        expect(muteButton.textContent).toBe("Mute Music");
    });
});
