const {
    setupDOM,
    loadScript,
    flushPromises,
    mockAudioController,
    setMockLocation
} = require("./testUtils");

describe("trivia.js", () => {
    let audioController;

    function buildDOM(questionMarkup = "") {
        setupDOM(`
            <input name="__RequestVerificationToken" value="token-321" />
            <div id="triviaResult" class="alert alert-secondary mb-4"></div>
            <form id="triviaForm" data-submit-url="/Minigames/SubmitTrivia" data-is-complete="false">
                <div id="triviaQuestionHost">${questionMarkup}</div>
                <button id="triviaSubmitButton" type="submit">Submit Answer</button>
                <button id="triviaMuteButton" type="button">Mute Music</button>
            </form>
            <div id="triviaCurrentPoints">0</div>
            <div id="triviaCorrectProgress">0 / 10</div>
            <div id="triviaDailyProgress">0 / 5</div>
        `);
    }

    function radioQuestion() {
        return `
            <fieldset class="trivia-question" data-question-id="q1" data-question-type="radio">
                <legend class="h5 mb-3">Question?</legend>
                <div class="form-check trivia-option">
                    <input class="form-check-input" type="radio" name="q1" id="q1-a" value="a" />
                    <label class="form-check-label" for="q1-a">A</label>
                </div>
                <div class="form-check trivia-option">
                    <input class="form-check-input" type="radio" name="q1" id="q1-b" value="b" />
                    <label class="form-check-label" for="q1-b">B</label>
                </div>
            </fieldset>
        `;
    }

    beforeEach(() => {
        jest.resetModules();
        audioController = mockAudioController();
        window.createMinigameAudio = jest.fn(() => audioController);
        global.fetch = jest.fn();
        setMockLocation();
    });

    afterEach(() => {
        jest.clearAllMocks();
    });

    test("exits without throwing if required DOM elements are missing", () => {
        setupDOM(`<div></div>`);
        expect(() => loadScript("trivia.js")).not.toThrow();
    });

    test("submitting with no selected answer shows warning and does not call fetch", () => {
        buildDOM(radioQuestion());
        loadScript("trivia.js");

        document.getElementById("triviaForm").dispatchEvent(new Event("submit", { bubbles: true, cancelable: true }));

        expect(fetch).not.toHaveBeenCalled();
        expect(document.getElementById("triviaResult").className).toContain("alert-warning");
    });

    test("radio answer submission posts JSON with anti-forgery token and starts audio", async () => {
        buildDOM(radioQuestion());
        global.fetch.mockResolvedValue({
            ok: true,
            json: async () => ({
                currentPoints: 5,
                correctAnswers: 1,
                correctAnswersToWin: 10,
                dailyPointsEarned: 0,
                dailyPointsLimit: 5,
                hasReachedDailyLimit: false,
                wasCorrect: true,
                awardedPoints: 0,
                isRoundComplete: false,
                nextQuestion: {
                    questionId: "q2",
                    prompt: "Next?",
                    questionType: "radio",
                    options: [{ optionKey: "x", label: "X" }]
                }
            })
        });

        buildDOM(radioQuestion());
        loadScript("trivia.js");

        document.getElementById("q1-a").checked = true;
        const form = document.getElementById("triviaForm");
        form.dispatchEvent(new Event("submit", { bubbles: true, cancelable: true }));

        expect(document.getElementById("triviaSubmitButton").disabled).toBe(true);
        expect(audioController.playIfNeeded).toHaveBeenCalledTimes(1);
        expect(fetch).toHaveBeenCalledWith("/Minigames/SubmitTrivia", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                RequestVerificationToken: "token-321"
            },
            body: JSON.stringify({
                questionId: "q1",
                selectedOptionKey: "a"
            })
        });

        await flushPromises();
    });

    test("later trivia submissions do not stop or restart music while under the daily limit", async () => {
        buildDOM(radioQuestion());
        global.fetch
            .mockResolvedValueOnce({
                ok: true,
                json: async () => ({
                    currentPoints: 5,
                    correctAnswers: 1,
                    correctAnswersToWin: 10,
                    dailyPointsEarned: 1,
                    dailyPointsLimit: 5,
                    hasReachedDailyLimit: false,
                    wasCorrect: true,
                    awardedPoints: 0,
                    isRoundComplete: false,
                    nextQuestion: {
                        questionId: "q2",
                        prompt: "Next?",
                        questionType: "radio",
                        options: [{ optionKey: "x", label: "X" }]
                    }
                })
            })
            .mockResolvedValueOnce({
                ok: true,
                json: async () => ({
                    currentPoints: 5,
                    correctAnswers: 2,
                    correctAnswersToWin: 10,
                    dailyPointsEarned: 2,
                    dailyPointsLimit: 5,
                    hasReachedDailyLimit: false,
                    wasCorrect: true,
                    awardedPoints: 0,
                    isRoundComplete: false,
                    nextQuestion: {
                        questionId: "q3",
                        prompt: "Next after next?",
                        questionType: "radio",
                        options: [{ optionKey: "z", label: "Z" }]
                    }
                })
            });

        loadScript("trivia.js");

        document.getElementById("q1-a").checked = true;
        document.getElementById("triviaForm").dispatchEvent(new Event("submit", { bubbles: true, cancelable: true }));
        await flushPromises();

        document.getElementById("q2-x").checked = true;
        document.getElementById("triviaForm").dispatchEvent(new Event("submit", { bubbles: true, cancelable: true }));
        await flushPromises();

        expect(audioController.playIfNeeded).toHaveBeenCalledTimes(2);
        expect(audioController.stop).not.toHaveBeenCalled();
    });

    test("correct answer updates progress and shows success message", async () => {
        buildDOM(radioQuestion());
        global.fetch.mockResolvedValue({
            ok: true,
            json: async () => ({
                currentPoints: 5,
                correctAnswers: 2,
                correctAnswersToWin: 10,
                dailyPointsEarned: 1,
                dailyPointsLimit: 5,
                hasReachedDailyLimit: false,
                wasCorrect: true,
                awardedPoints: 0,
                isRoundComplete: false,
                nextQuestion: {
                    questionId: "q2",
                    prompt: "Next?",
                    questionType: "radio",
                    options: [{ optionKey: "x", label: "X" }]
                }
            })
        });

        loadScript("trivia.js");
        document.getElementById("q1-a").checked = true;
        document.getElementById("triviaForm").dispatchEvent(new Event("submit", { bubbles: true, cancelable: true }));
        await flushPromises();

        expect(document.getElementById("triviaCurrentPoints").textContent).toBe("5");
        expect(document.getElementById("triviaCorrectProgress").textContent).toBe("2 / 10");
        expect(document.getElementById("triviaDailyProgress").textContent).toBe("1 / 5");
        expect(document.getElementById("triviaResult").className).toContain("alert-success");
        expect(document.getElementById("triviaSubmitButton").disabled).toBe(false);
    });

    test("incorrect answer shows warning message", async () => {
        buildDOM(radioQuestion());
        global.fetch.mockResolvedValue({
            ok: true,
            json: async () => ({
                currentPoints: 5,
                correctAnswers: 0,
                correctAnswersToWin: 10,
                dailyPointsEarned: 0,
                dailyPointsLimit: 5,
                hasReachedDailyLimit: false,
                wasCorrect: false,
                awardedPoints: 0,
                isRoundComplete: false,
                nextQuestion: {
                    questionId: "q2",
                    prompt: "Next?",
                    questionType: "radio",
                    options: [{ optionKey: "x", label: "X" }]
                }
            })
        });

        loadScript("trivia.js");
        document.getElementById("q1-a").checked = true;
        document.getElementById("triviaForm").dispatchEvent(new Event("submit", { bubbles: true, cancelable: true }));
        await flushPromises();

        expect(document.getElementById("triviaResult").className).toContain("alert-warning");
    });

    test("renders returned next radio question and re-enables submit", async () => {
        buildDOM(radioQuestion());
        global.fetch.mockResolvedValue({
            ok: true,
            json: async () => ({
                currentPoints: 5,
                correctAnswers: 1,
                correctAnswersToWin: 10,
                dailyPointsEarned: 0,
                dailyPointsLimit: 5,
                hasReachedDailyLimit: false,
                wasCorrect: true,
                awardedPoints: 0,
                isRoundComplete: false,
                nextQuestion: {
                    questionId: "q2",
                    prompt: "Radio next?",
                    questionType: "radio",
                    options: [
                        { optionKey: "x", label: "X" },
                        { optionKey: "y", label: "Y" }
                    ]
                }
            })
        });

        loadScript("trivia.js");
        document.getElementById("q1-a").checked = true;
        document.getElementById("triviaForm").dispatchEvent(new Event("submit", { bubbles: true, cancelable: true }));
        await flushPromises();

        expect(document.querySelector(".trivia-question").dataset.questionId).toBe("q2");
        expect(document.querySelectorAll("input[type='radio']")).toHaveLength(2);
        expect(document.getElementById("triviaSubmitButton").disabled).toBe(false);
    });

    test("renders returned dropdown question", async () => {
        buildDOM(radioQuestion());
        global.fetch.mockResolvedValue({
            ok: true,
            json: async () => ({
                currentPoints: 5,
                correctAnswers: 1,
                correctAnswersToWin: 10,
                dailyPointsEarned: 0,
                dailyPointsLimit: 5,
                hasReachedDailyLimit: false,
                wasCorrect: true,
                awardedPoints: 0,
                isRoundComplete: false,
                nextQuestion: {
                    questionId: "q3",
                    prompt: "Dropdown next?",
                    questionType: "dropdown",
                    options: [{ optionKey: "x", label: "X" }]
                }
            })
        });

        loadScript("trivia.js");
        document.getElementById("q1-a").checked = true;
        document.getElementById("triviaForm").dispatchEvent(new Event("submit", { bubbles: true, cancelable: true }));
        await flushPromises();

        expect(document.querySelector("select")).not.toBeNull();
    });

    test("renders returned text question", async () => {
        buildDOM(radioQuestion());
        global.fetch.mockResolvedValue({
            ok: true,
            json: async () => ({
                currentPoints: 5,
                correctAnswers: 1,
                correctAnswersToWin: 10,
                dailyPointsEarned: 0,
                dailyPointsLimit: 5,
                hasReachedDailyLimit: false,
                wasCorrect: true,
                awardedPoints: 0,
                isRoundComplete: false,
                nextQuestion: {
                    questionId: "q4",
                    prompt: "Text next?",
                    questionType: "text",
                    textPlaceholder: "Type",
                    options: []
                }
            })
        });

        loadScript("trivia.js");
        document.getElementById("q1-a").checked = true;
        document.getElementById("triviaForm").dispatchEvent(new Event("submit", { bubbles: true, cancelable: true }));
        await flushPromises();

        expect(document.querySelector("input[type='text']")).not.toBeNull();
    });

    test("round complete replaces question host and leaves submit disabled", async () => {
        buildDOM(radioQuestion());
        global.fetch.mockResolvedValue({
            ok: true,
            json: async () => ({
                currentPoints: 10,
                correctAnswers: 10,
                correctAnswersToWin: 10,
                dailyPointsEarned: 5,
                dailyPointsLimit: 5,
                hasReachedDailyLimit: true,
                wasCorrect: true,
                awardedPoints: 5,
                isRoundComplete: true,
                nextQuestion: null
            })
        });

        loadScript("trivia.js");
        document.getElementById("q1-a").checked = true;
        document.getElementById("triviaForm").dispatchEvent(new Event("submit", { bubbles: true, cancelable: true }));
        await flushPromises();

        expect(document.getElementById("triviaForm").dataset.isComplete).toBe("true");
        expect(document.getElementById("triviaQuestionHost").textContent).toContain("Round complete");
        expect(document.getElementById("triviaSubmitButton").disabled).toBe(true);
        expect(audioController.stop).toHaveBeenCalledTimes(1);
    });

    test("reaching the daily limit stops trivia music immediately", async () => {
        buildDOM(radioQuestion());
        global.fetch.mockResolvedValue({
            ok: true,
            json: async () => ({
                currentPoints: 10,
                correctAnswers: 10,
                correctAnswersToWin: 10,
                dailyPointsEarned: 5,
                dailyPointsLimit: 5,
                hasReachedDailyLimit: true,
                wasCorrect: true,
                awardedPoints: 5,
                isRoundComplete: false,
                nextQuestion: {
                    questionId: "q2",
                    prompt: "Unused next?",
                    questionType: "radio",
                    options: [{ optionKey: "x", label: "X" }]
                }
            })
        });

        loadScript("trivia.js");
        document.getElementById("q1-a").checked = true;
        document.getElementById("triviaForm").dispatchEvent(new Event("submit", { bubbles: true, cancelable: true }));
        await flushPromises();

        expect(document.getElementById("triviaDailyProgress").textContent).toBe("5 / 5");
        expect(audioController.stop).toHaveBeenCalledTimes(1);
    });

    test("if form is already complete submit does nothing", () => {
        buildDOM(radioQuestion());
        document.getElementById("triviaForm").dataset.isComplete = "true";
        loadScript("trivia.js");

        document.getElementById("q1-a").checked = true;
        document.getElementById("triviaForm").dispatchEvent(new Event("submit", { bubbles: true, cancelable: true }));

        expect(fetch).not.toHaveBeenCalled();
    });

    test("daily limit on page load prevents trivia music from starting", async () => {
        buildDOM(radioQuestion());
        document.getElementById("triviaDailyProgress").textContent = "5 / 5";
        global.fetch.mockResolvedValue({
            ok: true,
            json: async () => ({
                currentPoints: 10,
                correctAnswers: 10,
                correctAnswersToWin: 10,
                dailyPointsEarned: 5,
                dailyPointsLimit: 5,
                hasReachedDailyLimit: true,
                wasCorrect: true,
                awardedPoints: 5,
                isRoundComplete: true,
                nextQuestion: null
            })
        });
        loadScript("trivia.js");

        document.getElementById("q1-a").checked = true;
        document.getElementById("triviaForm").dispatchEvent(new Event("submit", { bubbles: true, cancelable: true }));
        await flushPromises();

        expect(audioController.playIfNeeded).not.toHaveBeenCalled();
    });

    test("401 redirects to login", async () => {
        buildDOM(radioQuestion());
        const consoleErrorSpy = jest.spyOn(console, "error").mockImplementation(() => { });
        global.fetch.mockResolvedValue({ ok: false, status: 401 });

        loadScript("trivia.js");
        document.getElementById("q1-a").checked = true;
        document.getElementById("triviaForm").dispatchEvent(new Event("submit", { bubbles: true, cancelable: true }));
        await flushPromises();

        expect(consoleErrorSpy).toHaveBeenCalled();
    });

    test("fetch failure shows danger message and re-enables submit", async () => {
        buildDOM(radioQuestion());
        jest.spyOn(console, "error").mockImplementation(() => { });
        global.fetch.mockRejectedValue(new Error("network"));

        loadScript("trivia.js");
        document.getElementById("q1-a").checked = true;
        document.getElementById("triviaForm").dispatchEvent(new Event("submit", { bubbles: true, cancelable: true }));
        await flushPromises();

        expect(document.getElementById("triviaResult").className).toContain("alert-danger");
        expect(document.getElementById("triviaSubmitButton").disabled).toBe(false);
    });

    test("mute button toggles text", () => {
        buildDOM(radioQuestion());
        audioController.toggleMute.mockReturnValueOnce(true).mockReturnValueOnce(false);
        loadScript("trivia.js");

        const muteButton = document.getElementById("triviaMuteButton");
        muteButton.click();
        expect(muteButton.textContent).toBe("Unmute Music");
        muteButton.click();
        expect(muteButton.textContent).toBe("Mute Music");
    });
});
