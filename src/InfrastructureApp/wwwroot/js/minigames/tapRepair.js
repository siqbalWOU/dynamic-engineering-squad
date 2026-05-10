(function () {
    const arena = document.getElementById("tapRepairArena");
    const resultElement = document.getElementById("tapRepairResult");
    const startButton = document.getElementById("tapRepairStartButton");
    const muteButton = document.getElementById("tapRepairMuteButton");
    const scoreElement = document.getElementById("tapRepairScore");
    const statusElement = document.getElementById("tapRepairStatus");
    const timerElement = document.getElementById("tapRepairTimer");
    const currentPointsElement = document.getElementById("tapRepairCurrentPoints");
    const dailyProgressElement = document.getElementById("tapRepairDailyProgress");

    if (!arena || !resultElement || !startButton || !scoreElement || !statusElement || !timerElement || !currentPointsElement || !dailyProgressElement) {
        return;
    }

    const audioController = window.createMinigameAudio
        ? window.createMinigameAudio({ src: "/audio/minigames/tap-repair-theme.mp3", label: "tap-repair-theme" })
        : null;

    const durationSeconds = Number.parseInt(arena.dataset.durationSeconds || "20", 10);
    const maxActivePotholes = 5;
    const spawnBurstCount = 2;
    const potholeLifetimeMs = 850;
    let score = 0;
    let timeRemaining = durationSeconds;
    let gameRunning = false;
    let spawnIntervalId = null;
    let countdownIntervalId = null;
    let completionSubmitted = false;
    let hasReachedDailyLimit = getHasReachedDailyLimit();

    startButton.addEventListener("click", startRound);

    if (muteButton && audioController) {
        muteButton.addEventListener("click", function () {
            const muted = audioController.toggleMute();
            muteButton.textContent = muted ? "Unmute Music" : "Mute Music";
        });
    }

    function startRound() {
        resetArena();
        score = 0;
        timeRemaining = durationSeconds;
        gameRunning = true;
        completionSubmitted = false;

        scoreElement.textContent = "0";
        timerElement.textContent = `${timeRemaining}s`;
        statusElement.textContent = "Repairing active potholes";
        resultElement.className = "alert alert-secondary mb-3";
        resultElement.textContent = "Round in progress. Potholes are spawning fast. Repair as many as you can before time runs out.";
        startButton.disabled = true;

        if (audioController && !hasReachedDailyLimit) {
            if (typeof audioController.playIfNeeded === "function") {
                audioController.playIfNeeded();
            } else {
                audioController.play();
            }
        }

        removeOverlay();
        spawnBurst();
        spawnIntervalId = window.setInterval(spawnBurst, 520);
        countdownIntervalId = window.setInterval(function () {
            timeRemaining -= 1;
            timerElement.textContent = `${Math.max(timeRemaining, 0)}s`;

            if (timeRemaining <= 0) {
                finishRound();
            }
        }, 1000);
    }

    function finishRound() {
        if (!gameRunning) {
            return;
        }

        gameRunning = false;
        clearTimers();
        clearPotholes();
        timerElement.textContent = "0s";
        statusElement.textContent = "Round complete";
        startButton.disabled = false;
        submitCompletion();
    }

    function spawnBurst() {
        const activePotholes = arena.querySelectorAll(".tap-repair-pothole").length;
        const availableSlots = Math.max(maxActivePotholes - activePotholes, 0);
        const potholesToSpawn = Math.min(spawnBurstCount, availableSlots);

        for (let index = 0; index < potholesToSpawn; index += 1) {
            spawnPothole();
        }
    }

    function spawnPothole() {
        if (!gameRunning) {
            return;
        }

        const pothole = document.createElement("button");
        pothole.type = "button";
        pothole.className = "tap-repair-pothole";
        pothole.setAttribute("aria-label", "Repair pothole");
        pothole.innerHTML = "<span class=\"tap-repair-pothole-core\"></span>";

        const maxLeft = Math.max(arena.clientWidth - 90, 0);
        const maxTop = Math.max(arena.clientHeight - 90, 0);
        pothole.style.left = `${Math.floor(Math.random() * (maxLeft + 1))}px`;
        pothole.style.top = `${Math.floor(Math.random() * (maxTop + 1))}px`;

        pothole.addEventListener("click", function () {
            if (!gameRunning) {
                return;
            }

            score += 1;
            scoreElement.textContent = String(score);
            statusElement.textContent = "Repair registered";
            pothole.remove();
        });

        arena.appendChild(pothole);

        window.setTimeout(function () {
            if (pothole.isConnected) {
                pothole.remove();
            }
        }, potholeLifetimeMs);
    }

    async function submitCompletion() {
        if (completionSubmitted) {
            return;
        }

        completionSubmitted = true;

        try {
            const response = await fetch(arena.dataset.completeUrl, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": getAntiForgeryToken()
                },
                body: JSON.stringify({
                    gameKey: arena.dataset.gameKey
                })
            });

            if (!response.ok) {
                if (response.status === 401) {
                    window.location.href = "/Account/Login";
                    return;
                }

                throw new Error("Tap Repair completion request failed.");
            }

            const data = await response.json();
            currentPointsElement.textContent = data.currentPoints;
            dailyProgressElement.textContent = `${data.dailyPointsEarned} / ${data.dailyPointsLimit}`;
            hasReachedDailyLimit = data.hasReachedDailyLimit === true
                || data.dailyPointsEarned >= data.dailyPointsLimit;

            if (data.awardedPoints > 0) {
                resultElement.className = "alert alert-success mb-3";
                resultElement.textContent = `Round complete. You repaired ${score} potholes and earned ${data.awardedPoints} point.`;
            } else {
                resultElement.className = "alert alert-warning mb-3";
                resultElement.textContent = `Round complete. You repaired ${score} potholes, but today's 5-point Tap Repair limit was already reached.`;
            }

            if (hasReachedDailyLimit && audioController) {
                audioController.stop();
            }
        } catch (error) {
            console.error(error);
            completionSubmitted = false;
            resultElement.className = "alert alert-danger mb-3";
            resultElement.textContent = "The round finished, but the reward could not be verified right now.";
        } finally {
            showOverlay("Repair Crew Reset", "Start another round whenever you're ready.");
        }
    }

    function clearTimers() {
        if (spawnIntervalId) {
            window.clearInterval(spawnIntervalId);
            spawnIntervalId = null;
        }

        if (countdownIntervalId) {
            window.clearInterval(countdownIntervalId);
            countdownIntervalId = null;
        }
    }

    function clearPotholes() {
        Array.from(arena.querySelectorAll(".tap-repair-pothole")).forEach(function (pothole) {
            pothole.remove();
        });
    }

    function resetArena() {
        clearTimers();
        clearPotholes();
    }

    function removeOverlay() {
        const overlay = arena.querySelector(".tap-repair-overlay");
        if (overlay) {
            overlay.remove();
        }
    }

    function showOverlay(title, subtitle) {
        removeOverlay();

        const overlay = document.createElement("div");
        overlay.className = "tap-repair-overlay";

        const heading = document.createElement("div");
        heading.className = "tap-repair-overlay-title";
        heading.textContent = title;

        const detail = document.createElement("div");
        detail.className = "text-muted";
        detail.textContent = subtitle;

        overlay.appendChild(heading);
        overlay.appendChild(detail);
        arena.appendChild(overlay);
    }

    function getAntiForgeryToken() {
        const field = document.querySelector('input[name="__RequestVerificationToken"]');
        return field ? field.value : "";
    }

    function getHasReachedDailyLimit() {
        const progressText = dailyProgressElement.textContent || "";
        const match = progressText.match(/(\d+)\s*\/\s*(\d+)/);
        if (!match) {
            return false;
        }

        return Number(match[1]) >= Number(match[2]);
    }

    window.addEventListener("beforeunload", function () {
        resetArena();
        if (audioController) {
            audioController.stop();
        }
    });
})();
