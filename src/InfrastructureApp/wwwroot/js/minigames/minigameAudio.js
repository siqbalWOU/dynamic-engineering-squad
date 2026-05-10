(function (window) {
    function createMinigameAudio(options) {
        const settings = options || {};
        const src = settings.src;
        const loop = settings.loop !== false;
        const label = settings.label || src || "minigame-audio";

        if (!src) {
            warn(`${label}: no src was provided. Audio will stay disabled.`);
            return createNoopController();
        }

        const audio = new Audio(src);
        audio.loop = loop;
        audio.preload = "none";

        let muted = false;
        let failed = false;
        let playing = false;

        audio.addEventListener("error", function () {
            failed = true;
            playing = false;
            warn(`${label}: failed to load audio from "${src}". Verify the file exists under wwwroot/audio/minigames and the URL is correct.`);
        });

        audio.addEventListener("ended", function () {
            playing = false;

            if (loop && !failed) {
                play();
            }
        });

        function play() {
            if (failed) {
                warn(`${label}: play() skipped because audio is already in a failed state.`);
                return;
            }

            audio.muted = muted;
            playing = true;
            const promise = audio.play();
            if (promise && typeof promise.catch === "function") {
                promise.catch(function (error) {
                    playing = false;
                    warn(`${label}: play() was rejected by the browser. This usually means autoplay/user-interaction rules blocked it, or the file could not be loaded.`, error);
                });
            }
        }

        function playIfNeeded() {
            if (playing) {
                return;
            }

            play();
        }

        function stop() {
            audio.pause();
            audio.currentTime = 0;
            playing = false;
        }

        function toggleMute() {
            muted = !muted;
            audio.muted = muted;
            return muted;
        }

        window.addEventListener("beforeunload", stop);
        window.addEventListener("pagehide", stop);
        document.addEventListener("visibilitychange", function () {
            if (document.visibilityState === "hidden") {
                audio.pause();
            }
        });

        return {
            play: play,
            playIfNeeded: playIfNeeded,
            stop: stop,
            toggleMute: toggleMute,
            isMuted: function () { return muted; },
            isPlaying: function () { return playing; }
        };
    }

    function createNoopController() {
        let muted = false;

        return {
            play: function () { },
            playIfNeeded: function () { },
            stop: function () { },
            toggleMute: function () {
                muted = !muted;
                return muted;
            },
            isMuted: function () { return muted; },
            isPlaying: function () { return false; }
        };
    }

    function warn(message, error) {
        if (window.console && typeof window.console.warn === "function") {
            if (error) {
                window.console.warn("[minigameAudio] " + message, error);
            } else {
                window.console.warn("[minigameAudio] " + message);
            }
        }
    }

    window.createMinigameAudio = createMinigameAudio;
})(window);
