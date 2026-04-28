/**
 * Report Assist Autocomplete
 * --------------------------
 * Handles real-time autocomplete suggestions for the report description textarea.
 *
 * Responsibilities:
 * - Listens for user input
 * - Extracts the current word being typed
 * - Calls backend API with debounce to limit requests
 * - Renders suggestions in a dropdown
 * - Supports keyboard navigation and mouse selection
 * - Replaces only the active word in the textarea
 */

document.addEventListener("DOMContentLoaded", () => {

    const descriptionInput = document.getElementById("Description");
    const suggestionsBox = document.getElementById("descriptionSuggestions");

    // Exit early if required elements are not present
    if (!descriptionInput || !suggestionsBox) return;

    let suggestions = [];
    let activeIndex = -1;     // Tracks highlighted suggestion (-1 = none)
    let debounceTimer = null; // Used to delay API calls while typing


    /**
     * Returns the last word currently being typed by the user.
     */
    function getCurrentWord(text) {
        const trimmedEnd = text.trimEnd();
        if (!trimmedEnd) return "";

        const words = trimmedEnd.split(/\s+/);
        return words[words.length - 1] || "";
    }


    /**
     * INPUT EVENT
     * Triggers autocomplete lookup with debounce.
     */
    descriptionInput.addEventListener("input", () => {
        clearTimeout(debounceTimer);

        const fullText = descriptionInput.value;
        const currentWord = getCurrentWord(fullText);

        // Only query API if current word has sufficient length
        if (currentWord.length < 2) {
            hideSuggestions();
            return;
        }

        debounceTimer = setTimeout(async () => {
            try {
                const response = await fetch(
                    `/api/reportassist/suggestions?q=${encodeURIComponent(currentWord)}`
                );

                if (!response.ok) {
                    hideSuggestions();
                    return;
                }

                suggestions = await response.json();
                activeIndex = -1;

                renderSuggestions(suggestions);
            } catch {
                hideSuggestions();
            }
        }, 200);
    });


    /**
     * KEYBOARD NAVIGATION
     * Supports ArrowUp, ArrowDown, Enter, and Escape.
     */
    descriptionInput.addEventListener("keydown", (e) => {
        if (suggestions.length === 0) return;

        switch (e.key) {
            case "ArrowDown":
                e.preventDefault();
                activeIndex = Math.min(activeIndex + 1, suggestions.length - 1);
                renderSuggestions(suggestions);
                break;

            case "ArrowUp":
                e.preventDefault();
                activeIndex = Math.max(activeIndex - 1, 0);
                renderSuggestions(suggestions);
                break;

            case "Enter":
                if (activeIndex >= 0) {
                    e.preventDefault();
                    applySuggestion(suggestions[activeIndex]);
                }
                break;

            case "Escape":
                hideSuggestions();
                break;
        }
    });


    /**
     * GLOBAL CLICK HANDLER
     * Closes dropdown when clicking outside.
     */
    document.addEventListener("click", (e) => {
        if (!suggestionsBox.contains(e.target) && e.target !== descriptionInput) {
            hideSuggestions();
        }
    });


    /**
     * Renders suggestion list into the dropdown.
     */
    function renderSuggestions(items) {
        suggestionsBox.innerHTML = "";

        if (!items || items.length === 0) {
            hideSuggestions();
            return;
        }

        items.forEach((item, index) => {
            const div = document.createElement("div");

            div.className = "autocomplete-item" +
                (index === activeIndex ? " active" : "");

            div.textContent = item;

            // Use mousedown to prevent textarea losing focus
            div.addEventListener("mousedown", (e) => {
                e.preventDefault();
                applySuggestion(item);
            });

            suggestionsBox.appendChild(div);
        });

        suggestionsBox.classList.remove("d-none");
    }


    /**
     * Applies the selected suggestion to the textarea.
     * Only replaces the last word being typed.
     */
    function applySuggestion(suggestion) {
        const currentText = descriptionInput.value;
        const trimmed = currentText.trimEnd();

        const lastSpaceIndex = trimmed.lastIndexOf(" ");

        const prefix = lastSpaceIndex >= 0
            ? trimmed.substring(0, lastSpaceIndex + 1)
            : "";

        descriptionInput.value = prefix + suggestion;

        // Trigger input event to update any dependent UI (e.g., word count)
        descriptionInput.dispatchEvent(new Event("input"));

        hideSuggestions();
        descriptionInput.focus();
    }


    /**
     * Clears and hides the suggestion dropdown.
     */
    function hideSuggestions() {
        suggestions = [];
        activeIndex = -1;
        suggestionsBox.innerHTML = "";
        suggestionsBox.classList.add("d-none");
    }
});