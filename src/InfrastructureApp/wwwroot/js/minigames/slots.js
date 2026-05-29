(function () {
    const symbolAssets = {
        pothole: { alt: "Pothole", src: "/Images/minigames/symbols/pothole.svg" },
        cone: { alt: "Traffic cone", src: "/Images/minigames/symbols/cone.svg" },
        "road-sign": { alt: "Road sign", src: "/Images/minigames/symbols/road-sign.svg" },
        "traffic-light": { alt: "Traffic light", src: "/Images/minigames/symbols/traffic-light.svg" },
        bridge: { alt: "Bridge", src: "/Images/minigames/symbols/bridge.svg" }
    };

    const spinButton = document.getElementById("slotsSpinButton");
    const muteButton = document.getElementById("slotsMuteButton");
    const resultElement = document.getElementById("slotsResult");
    const currentPointsElement = document.getElementById("slotsCurrentPoints");
    const dailyProgressElement = document.getElementById("slotsDailyProgress");
    const reelElements = Array.from(document.querySelectorAll("[data-slot-reel]"));

    if (!spinButton || !resultElement || !currentPointsElement || !dailyProgressElement || reelElements.length !== 3) {
        return;
    }

    const audioController = window.createMinigameAudio
        ? window.createMinigameAudio({ src: "/audio/minigames/slots-theme.mp3", label: "slots-theme" })
        : null;
    let hasStartedMusic = false;
    let hasReachedDailyLimit = spinButton.disabled;

    spinButton.addEventListener("click", async function () {
        spinButton.disabled = true;

        if (audioController && !hasReachedDailyLimit) {
            if (typeof audioController.playIfNeeded === "function") {
                audioController.playIfNeeded();
            } else if (!hasStartedMusic) {
                audioController.play();
            }

            hasStartedMusic = true;
        }

        try {
            const response = await fetch(spinButton.dataset.spinUrl, {
                method: "POST",
                headers: {
                    "RequestVerificationToken": getAntiForgeryToken()
                }
            });

            if (!response.ok) {
                if (response.status === 401) {
                    window.location.href = "/Account/Login";
                    return;
                }

                throw new Error("Spin request failed.");
            }

            const data = await response.json();
            renderSymbols(data.symbols || []);
            renderResult(data);
            currentPointsElement.textContent = data.currentPoints;
            dailyProgressElement.textContent = `${data.dailyPointsEarned} / ${data.dailyPointsLimit}`;
            hasReachedDailyLimit = data.hasReachedDailyLimit === true;

            if (hasReachedDailyLimit && audioController) {
                audioController.stop();
                hasStartedMusic = false;
            }

            if (!hasReachedDailyLimit) {
                spinButton.disabled = false;
            }
        } catch (error) {
            console.error(error);
            resultElement.className = "alert alert-danger mb-3";
            resultElement.textContent = "The slot spin could not be completed right now.";
            spinButton.disabled = false;
        }
    });

    if (muteButton && audioController) {
        muteButton.addEventListener("click", function () {
            const muted = audioController.toggleMute();
            muteButton.textContent = muted ? "Unmute Music" : "Mute Music";
        });
    }

    function renderSymbols(symbols) {
        reelElements.forEach(function (reel, index) {
            const symbol = symbols[index] || "?";
            reel.replaceChildren(createReelContent(symbol));
        });
    }

    function renderResult(data) {
        if (data.hasReachedDailyLimit && data.awardedPoints === 0) {
            resultElement.className = "alert alert-warning mb-3";
            resultElement.textContent = "You already reached today's 5-point Slots limit.";
            return;
        }

        if (data.isWinningSpin) {
            resultElement.className = "alert alert-success mb-3";
            resultElement.textContent = `${data.resultLabel}. You earned ${data.awardedPoints} point.`;
            return;
        }

        resultElement.className = "alert alert-secondary mb-3";
        resultElement.textContent = "No match this spin. Try again.";
    }

    function createReelContent(symbol) {
        const asset = symbolAssets[symbol];
        if (!asset) {
            const fallback = document.createElement("span");
            fallback.className = "slots-reel-placeholder";
            fallback.textContent = symbol;
            return fallback;
        }

        const image = document.createElement("img");
        image.className = "slots-symbol-image";
        image.src = asset.src;
        image.alt = asset.alt;
        return image;
    }

    function getAntiForgeryToken() {
        const field = document.querySelector('input[name="__RequestVerificationToken"]');
        return field ? field.value : "";
    }
})();
