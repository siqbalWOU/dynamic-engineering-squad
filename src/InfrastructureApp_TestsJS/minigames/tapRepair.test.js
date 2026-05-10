const {
    setupDOM,
    loadScript,
    flushPromises,
    mockAudioController,
    setMockLocation
} = require("./testUtils");

describe("tapRepair.js", () => {
    let audioController;

    function buildDOM() {
        setupDOM(`
            <input name="__RequestVerificationToken" value="token-789" />
            <div id="tapRepairResult" class="alert alert-secondary mb-3"></div>
            <button id="tapRepairStartButton" type="button">Start Repair Run</button>
            <button id="tapRepairMuteButton" type="button">Mute Music</button>
            <div id="tapRepairScore">5</div>
            <div id="tapRepairStatus">Waiting to start</div>
            <div id="tapRepairTimer">20s</div>
            <div id="tapRepairCurrentPoints">0</div>
            <div id="tapRepairDailyProgress">0 / 5</div>
            <div id="tapRepairArena" data-complete-url="/Minigames/CompleteGame" data-game-key="tap-repair" data-duration-seconds="3">
                <div class="tap-repair-overlay">
                    <div class="tap-repair-overlay-title">Repair Crew Ready</div>
                </div>
            </div>
        `);

        const arena = document.getElementById("tapRepairArena");
        Object.defineProperty(arena, "clientWidth", { configurable: true, value: 500 });
        Object.defineProperty(arena, "clientHeight", { configurable: true, value: 400 });
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
        expect(() => loadScript("tapRepair.js")).not.toThrow();
    });

    test("clicking Start resets state, starts audio, and spawns potholes", () => {
        buildDOM();
        loadScript("tapRepair.js");

        document.getElementById("tapRepairStartButton").click();

        expect(document.getElementById("tapRepairScore").textContent).toBe("0");
        expect(document.getElementById("tapRepairTimer").textContent).toBe("3s");
        expect(document.getElementById("tapRepairStartButton").disabled).toBe(true);
        expect(document.getElementById("tapRepairStatus").textContent).toContain("Repairing active potholes");
        expect(document.getElementById("tapRepairResult").textContent).toContain("Round in progress");
        expect(audioController.playIfNeeded).toHaveBeenCalledTimes(1);
        expect(document.querySelectorAll(".tap-repair-pothole").length).toBeGreaterThan(0);
    });

    test("clicking a pothole increments score and removes it", () => {
        buildDOM();
        loadScript("tapRepair.js");
        document.getElementById("tapRepairStartButton").click();

        const pothole = document.querySelector(".tap-repair-pothole");
        pothole.click();

        expect(document.getElementById("tapRepairScore").textContent).toBe("1");
        expect(document.getElementById("tapRepairStatus").textContent).toContain("Repair registered");
        expect(document.querySelectorAll(".tap-repair-pothole").length).toBe(1);
    });

    test("potholes remove themselves after lifetime timer", () => {
        buildDOM();
        loadScript("tapRepair.js");
        document.getElementById("tapRepairStartButton").click();

        const initialPotholes = [...document.querySelectorAll(".tap-repair-pothole")];
        expect(initialPotholes).toHaveLength(2);
        jest.advanceTimersByTime(850);
        initialPotholes.forEach((pothole) => {
            expect(pothole.isConnected).toBe(false);
        });
    });

    test("timer ending submits completion, clears potholes, and re-enables start", async () => {
        buildDOM();
        global.fetch.mockResolvedValue({
            ok: true,
            json: async () => ({
                currentPoints: 20,
                dailyPointsEarned: 2,
                dailyPointsLimit: 5,
                awardedPoints: 1,
                hasReachedDailyLimit: false
            })
        });

        loadScript("tapRepair.js");
        document.getElementById("tapRepairStartButton").click();

        jest.advanceTimersByTime(3000);
        await flushPromises();

        expect(fetch).toHaveBeenCalledWith("/Minigames/CompleteGame", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                RequestVerificationToken: "token-789"
            },
            body: JSON.stringify({
                gameKey: "tap-repair"
            })
        });
        expect(document.getElementById("tapRepairTimer").textContent).toBe("0s");
        expect(document.getElementById("tapRepairStartButton").disabled).toBe(false);
        expect(document.querySelectorAll(".tap-repair-pothole")).toHaveLength(0);
        expect(document.getElementById("tapRepairCurrentPoints").textContent).toBe("20");
        expect(document.getElementById("tapRepairDailyProgress").textContent).toBe("2 / 5");
        expect(document.getElementById("tapRepairResult").className).toContain("alert-success");
        expect(audioController.stop).not.toHaveBeenCalled();
    });

    test("awarded points 0 shows warning message", async () => {
        buildDOM();
        global.fetch.mockResolvedValue({
            ok: true,
            json: async () => ({
                currentPoints: 20,
                dailyPointsEarned: 5,
                dailyPointsLimit: 5,
                awardedPoints: 0,
                hasReachedDailyLimit: true
            })
        });

        loadScript("tapRepair.js");
        document.getElementById("tapRepairStartButton").click();
        jest.advanceTimersByTime(3000);
        await flushPromises();

        expect(document.getElementById("tapRepairResult").className).toContain("alert-warning");
        expect(audioController.stop).toHaveBeenCalledTimes(1);
    });

    test("fetch failure shows danger message", async () => {
        buildDOM();
        jest.spyOn(console, "error").mockImplementation(() => { });
        global.fetch.mockRejectedValue(new Error("network"));

        loadScript("tapRepair.js");
        document.getElementById("tapRepairStartButton").click();
        jest.advanceTimersByTime(3000);
        await flushPromises();

        expect(document.getElementById("tapRepairResult").className).toContain("alert-danger");
        expect(audioController.stop).not.toHaveBeenCalled();
    });

    test("401 redirects to login", async () => {
        buildDOM();
        const consoleErrorSpy = jest.spyOn(console, "error").mockImplementation(() => { });
        global.fetch.mockResolvedValue({ ok: false, status: 401 });

        loadScript("tapRepair.js");
        document.getElementById("tapRepairStartButton").click();
        jest.advanceTimersByTime(3000);
        await flushPromises();

        expect(consoleErrorSpy).toHaveBeenCalled();
        expect(audioController.stop).not.toHaveBeenCalled();
    });

    test("later rounds do not stop or restart music while under the daily limit", async () => {
        buildDOM();
        global.fetch
            .mockResolvedValueOnce({
                ok: true,
                json: async () => ({
                    currentPoints: 20,
                    dailyPointsEarned: 1,
                    dailyPointsLimit: 5,
                    awardedPoints: 1,
                    hasReachedDailyLimit: false
                })
            })
            .mockResolvedValueOnce({
                ok: true,
                json: async () => ({
                    currentPoints: 21,
                    dailyPointsEarned: 2,
                    dailyPointsLimit: 5,
                    awardedPoints: 1,
                    hasReachedDailyLimit: false
                })
            });

        loadScript("tapRepair.js");

        document.getElementById("tapRepairStartButton").click();
        jest.advanceTimersByTime(3000);
        await flushPromises();

        document.getElementById("tapRepairStartButton").click();
        jest.advanceTimersByTime(3000);
        await flushPromises();

        expect(audioController.playIfNeeded).toHaveBeenCalledTimes(2);
        expect(audioController.stop).not.toHaveBeenCalled();
    });

    test("daily limit on page load prevents Tap Repair music from starting", () => {
        buildDOM();
        document.getElementById("tapRepairDailyProgress").textContent = "5 / 5";
        loadScript("tapRepair.js");

        document.getElementById("tapRepairStartButton").click();

        expect(audioController.playIfNeeded).not.toHaveBeenCalled();
    });

    test("beforeunload clears arena and stops audio", () => {
        buildDOM();
        loadScript("tapRepair.js");
        document.getElementById("tapRepairStartButton").click();

        expect(document.querySelectorAll(".tap-repair-pothole").length).toBeGreaterThan(0);
        window.dispatchEvent(new Event("beforeunload"));

        expect(document.querySelectorAll(".tap-repair-pothole")).toHaveLength(0);
        expect(audioController.stop).toHaveBeenCalledTimes(1);
    });

    test("mute button toggles text", () => {
        buildDOM();
        audioController.toggleMute.mockReturnValueOnce(true).mockReturnValueOnce(false);
        loadScript("tapRepair.js");

        const muteButton = document.getElementById("tapRepairMuteButton");
        muteButton.click();
        expect(muteButton.textContent).toBe("Unmute Music");
        muteButton.click();
        expect(muteButton.textContent).toBe("Mute Music");
    });
});
