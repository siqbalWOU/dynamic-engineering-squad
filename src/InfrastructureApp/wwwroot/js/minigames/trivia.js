(function () {
    const form = document.getElementById("triviaForm");
    const questionHost = document.getElementById("triviaQuestionHost");
    const resultElement = document.getElementById("triviaResult");
    const submitButton = document.getElementById("triviaSubmitButton");
    const muteButton = document.getElementById("triviaMuteButton");
    const currentPointsElement = document.getElementById("triviaCurrentPoints");
    const correctProgressElement = document.getElementById("triviaCorrectProgress");
    const dailyProgressElement = document.getElementById("triviaDailyProgress");

    if (!form || !questionHost || !resultElement || !submitButton || !currentPointsElement || !correctProgressElement || !dailyProgressElement) {
        return;
    }

    const audioController = window.createMinigameAudio
        ? window.createMinigameAudio({ src: "/audio/minigames/trivia-theme.mp3", label: "trivia-theme" })
        : null;
    let hasReachedDailyLimit = getHasReachedDailyLimit();

    if (muteButton && audioController) {
        muteButton.addEventListener("click", function () {
            const muted = audioController.toggleMute();
            muteButton.textContent = muted ? "Unmute Music" : "Mute Music";
        });
    }

    form.addEventListener("submit", async function (event) {
        event.preventDefault();

        if (form.dataset.isComplete === "true") {
            return;
        }

        const answer = collectAnswer();
        if (!answer) {
            resultElement.className = "alert alert-warning mb-4";
            resultElement.textContent = "Choose or enter an answer before submitting.";
            return;
        }

        submitButton.disabled = true;
        if (audioController && !hasReachedDailyLimit) {
            if (typeof audioController.playIfNeeded === "function") {
                audioController.playIfNeeded();
            } else {
                audioController.play();
            }
        }

        try {
            const response = await fetch(form.dataset.submitUrl, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": getAntiForgeryToken()
                },
                body: JSON.stringify(answer)
            });

            if (!response.ok) {
                if (response.status === 401) {
                    window.location.href = "/Account/Login";
                    return;
                }

                throw new Error("Trivia submission failed.");
            }

            const data = await response.json();
            currentPointsElement.textContent = data.currentPoints;
            correctProgressElement.textContent = `${data.correctAnswers} / ${data.correctAnswersToWin}`;
            dailyProgressElement.textContent = `${data.dailyPointsEarned} / ${data.dailyPointsLimit}`;
            hasReachedDailyLimit = data.hasReachedDailyLimit === true
                || data.dailyPointsEarned >= data.dailyPointsLimit;

            if (data.wasCorrect) {
                resultElement.className = "alert alert-success mb-4";
                resultElement.textContent = data.awardedPoints > 0
                    ? "Correct. You reached 10 correct answers and earned 5 points."
                    : "Correct. Moving to the next question.";
            } else {
                resultElement.className = "alert alert-warning mb-4";
                resultElement.textContent = "Incorrect. Moving to the next question.";
            }

            if (data.isRoundComplete || !data.nextQuestion) {
                form.dataset.isComplete = "true";
                questionHost.innerHTML = "<div class=\"trivia-complete-panel\">Round complete. Come back tomorrow for another reward run.</div>";
                submitButton.disabled = true;

                if (hasReachedDailyLimit && audioController) {
                    audioController.stop();
                }

                return;
            }

            if (hasReachedDailyLimit && audioController) {
                audioController.stop();
            }

            renderQuestion(data.nextQuestion);
            submitButton.disabled = false;
        } catch (error) {
            console.error(error);
            submitButton.disabled = false;
            resultElement.className = "alert alert-danger mb-4";
            resultElement.textContent = "The trivia answer could not be checked right now.";
        }
    });

    function collectAnswer() {
        const questionElement = questionHost.querySelector(".trivia-question");
        if (!questionElement) {
            return null;
        }

        const questionType = questionElement.dataset.questionType;
        let selectedOptionKey = "";

        if (questionType === "text") {
            const textInput = questionElement.querySelector("input[type='text']");
            selectedOptionKey = textInput ? textInput.value.trim() : "";
        } else if (questionType === "dropdown") {
            const select = questionElement.querySelector("select");
            selectedOptionKey = select ? select.value : "";
        } else {
            const selectedInput = questionElement.querySelector("input[type='radio']:checked");
            selectedOptionKey = selectedInput ? selectedInput.value : "";
        }

        if (!selectedOptionKey) {
            return null;
        }

        return {
            questionId: questionElement.dataset.questionId,
            selectedOptionKey: selectedOptionKey
        };
    }

    function renderQuestion(question) {
        questionHost.innerHTML = "";

        const fieldset = document.createElement("fieldset");
        fieldset.className = "trivia-question";
        fieldset.dataset.questionId = question.questionId;
        fieldset.dataset.questionType = question.questionType;

        const legend = document.createElement("legend");
        legend.className = "h5 mb-3";
        legend.textContent = question.prompt;
        fieldset.appendChild(legend);

        if (question.questionType === "text") {
            const input = document.createElement("input");
            input.className = "form-control form-control-lg";
            input.type = "text";
            input.id = "triviaTextAnswer";
            input.placeholder = question.textPlaceholder || "";
            input.autocomplete = "off";
            fieldset.appendChild(input);
        } else if (question.questionType === "dropdown") {
            const label = document.createElement("label");
            label.className = "form-label";
            label.htmlFor = "triviaDropdownAnswer";
            label.textContent = "Choose one answer";
            fieldset.appendChild(label);

            const select = document.createElement("select");
            select.className = "form-select form-select-lg";
            select.id = "triviaDropdownAnswer";

            const placeholderOption = document.createElement("option");
            placeholderOption.value = "";
            placeholderOption.textContent = "Select an answer";
            select.appendChild(placeholderOption);

            question.options.forEach(function (option) {
                const optionElement = document.createElement("option");
                optionElement.value = option.optionKey;
                optionElement.textContent = option.label;
                select.appendChild(optionElement);
            });

            fieldset.appendChild(select);
        } else {
            question.options.forEach(function (option) {
                const wrapper = document.createElement("div");
                wrapper.className = "form-check trivia-option";

                const input = document.createElement("input");
                input.className = "form-check-input";
                input.type = "radio";
                input.name = question.questionId;
                input.id = `${question.questionId}-${option.optionKey}`;
                input.value = option.optionKey;

                const label = document.createElement("label");
                label.className = "form-check-label";
                label.htmlFor = input.id;
                label.textContent = option.label;

                wrapper.appendChild(input);
                wrapper.appendChild(label);
                fieldset.appendChild(wrapper);
            });
        }

        questionHost.appendChild(fieldset);
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
})();
