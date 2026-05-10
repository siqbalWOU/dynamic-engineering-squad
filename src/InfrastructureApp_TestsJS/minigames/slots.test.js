const {
    setupDOM,
    loadScript,
    flushPromises,
    mockAudioController,
    setMockLocation
} = require("./testUtils");

describe("slots.js", () => {
    let audioController;

    function buildDOM() {
        setupDOM(`
            <input name="__RequestVerificationToken" value="token-123" />
            <button id="slotsSpinButton" data-spin-url="/Minigames/SpinSlots">Spin</button>
            <button id="slotsMuteButton" type="button">Mute Music</button>
            <div id="slotsResult" class="alert alert-secondary mb-3"></div>
            <div id="slotsCurrentPoints">0</div>
            <div id="slotsDailyProgress">0 / 5</div>
            <div data-slot-reel>?</div>
            <div data-slot-reel>?</div>
            <div data-slot-reel>?</div>
        `);
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

        expect(() => loadScript("slots.js")).not.toThrow();
    });

    test("clicking Spin posts with anti-forgery header, updates DOM, and starts persistent audio", async () => {
        buildDOM();
        global.fetch.mockResolvedValue({
            ok: true,
            json: async () => ({
                symbols: ["pothole", "road-sign", "traffic-light"],
                currentPoints: 7,
                dailyPointsEarned: 2,
                dailyPointsLimit: 5,
                hasReachedDailyLimit: false,
                isWinningSpin: false,
                awardedPoints: 0,
                resultLabel: "Road Crew Spin"
            })
        });

        loadScript("slots.js");
        const spinButton = document.getElementById("slotsSpinButton");
        spinButton.click();

        expect(spinButton.disabled).toBe(true);
        expect(audioController.playIfNeeded).toHaveBeenCalledTimes(1);
        expect(fetch).toHaveBeenCalledWith("/Minigames/SpinSlots", {
            method: "POST",
            headers: {
                RequestVerificationToken: "token-123"
            }
        });

        await flushPromises();

        const reels = [...document.querySelectorAll("[data-slot-reel]")];
        const reelImages = reels.map((reel) => reel.querySelector("img"));
        expect(reelImages.map((image) => image.alt)).toEqual(["Pothole", "Road sign", "Traffic light"]);
        expect(reelImages.map((image) => image.getAttribute("src"))).toEqual([
            "/Images/minigames/symbols/pothole.svg",
            "/Images/minigames/symbols/road-sign.svg",
            "/Images/minigames/symbols/traffic-light.svg"
        ]);
        expect(document.getElementById("slotsCurrentPoints").textContent).toBe("7");
        expect(document.getElementById("slotsDailyProgress").textContent).toBe("2 / 5");
        expect(document.getElementById("slotsResult").textContent).toContain("No match this spin");
        expect(spinButton.disabled).toBe(false);
        expect(audioController.stop).not.toHaveBeenCalled();
    });

    test("winning response shows success alert", async () => {
        buildDOM();
        global.fetch.mockResolvedValue({
            ok: true,
            json: async () => ({
                symbols: ["bridge", "bridge", "bridge"],
                currentPoints: 9,
                dailyPointsEarned: 1,
                dailyPointsLimit: 5,
                hasReachedDailyLimit: false,
                isWinningSpin: true,
                awardedPoints: 1,
                resultLabel: "Three of a Kind"
            })
        });

        loadScript("slots.js");
        document.getElementById("slotsSpinButton").click();
        await flushPromises();

        const result = document.getElementById("slotsResult");
        expect(result.className).toContain("alert-success");
        expect(result.textContent).toContain("Three of a Kind");
    });

    test("already at daily limit shows warning and keeps button disabled", async () => {
        buildDOM();
        global.fetch.mockResolvedValue({
            ok: true,
            json: async () => ({
                symbols: ["cone", "cone", "cone"],
                currentPoints: 15,
                dailyPointsEarned: 5,
                dailyPointsLimit: 5,
                hasReachedDailyLimit: true,
                isWinningSpin: false,
                awardedPoints: 0,
                resultLabel: "Limit"
            })
        });

        loadScript("slots.js");
        const spinButton = document.getElementById("slotsSpinButton");
        spinButton.click();
        await flushPromises();

        expect(document.getElementById("slotsResult").className).toContain("alert-warning");
        expect(spinButton.disabled).toBe(true);
        expect(audioController.stop).toHaveBeenCalledTimes(1);
    });

    test("401 redirects to login", async () => {
        buildDOM();
        const consoleErrorSpy = jest.spyOn(console, "error").mockImplementation(() => { });
        global.fetch.mockResolvedValue({ ok: false, status: 401 });

        loadScript("slots.js");
        document.getElementById("slotsSpinButton").click();
        await flushPromises();

        expect(consoleErrorSpy).toHaveBeenCalled();
        expect(audioController.stop).not.toHaveBeenCalled();
    });

    test("fetch failure shows danger alert and re-enables button", async () => {
        buildDOM();
        jest.spyOn(console, "error").mockImplementation(() => { });
        global.fetch.mockRejectedValue(new Error("network"));

        loadScript("slots.js");
        const spinButton = document.getElementById("slotsSpinButton");
        spinButton.click();
        await flushPromises();

        expect(document.getElementById("slotsResult").className).toContain("alert-danger");
        expect(spinButton.disabled).toBe(false);
        expect(audioController.stop).not.toHaveBeenCalled();
    });

    test("later spins do not restart or stack audio playback", async () => {
        buildDOM();
        global.fetch
            .mockResolvedValueOnce({
                ok: true,
                json: async () => ({
                    symbols: ["pothole", "road-sign", "traffic-light"],
                    currentPoints: 7,
                    dailyPointsEarned: 2,
                    dailyPointsLimit: 5,
                    hasReachedDailyLimit: false,
                    isWinningSpin: false,
                    awardedPoints: 0,
                    resultLabel: "Road Crew Spin"
                })
            })
            .mockResolvedValueOnce({
                ok: true,
                json: async () => ({
                    symbols: ["bridge", "bridge", "bridge"],
                    currentPoints: 8,
                    dailyPointsEarned: 3,
                    dailyPointsLimit: 5,
                    hasReachedDailyLimit: false,
                    isWinningSpin: true,
                    awardedPoints: 1,
                    resultLabel: "Three of a Kind"
                })
            });

        loadScript("slots.js");
        const spinButton = document.getElementById("slotsSpinButton");
        spinButton.click();
        await flushPromises();
        spinButton.click();
        await flushPromises();

        expect(audioController.playIfNeeded).toHaveBeenCalledTimes(2);
        expect(audioController.stop).not.toHaveBeenCalled();
    });

    test("mute button toggles text", () => {
        buildDOM();
        audioController.toggleMute.mockReturnValueOnce(true).mockReturnValueOnce(false);

        loadScript("slots.js");
        const muteButton = document.getElementById("slotsMuteButton");
        muteButton.click();
        expect(muteButton.textContent).toBe("Unmute Music");

        muteButton.click();
        expect(muteButton.textContent).toBe("Mute Music");
    });
});
