// This JS file watches user typing, calls the backend API, displays suggestions
// in a dropdown, lets the user interact with suggestions, and updates only part
// of the text.
//
// Debouncing is used to prevent too many API calls.
// It waits a little bit while the user is typing before sending the request.



// Wait until the entire HTML page is loaded before running this script
document.addEventListener("DOMContentLoaded", () => {

    // Get the textarea where the user types the report description
    const descriptionInput = document.getElementById("Description");

    // Get the container where autocomplete suggestions will appear
    const suggestionsBox = document.getElementById("descriptionSuggestions");

    // If either element is missing, stop the script
    if (!descriptionInput || !suggestionsBox) return;

    // Holds the current list of suggestions returned from the server
    let suggestions = [];

    // Tracks which suggestion is currently highlighted for keyboard navigation
    // -1 means nothing is selected
    let activeIndex = -1;

    // Used for debouncing API calls while typing
    let debounceTimer = null;


    // FUNCTION: Get the last word the user is currently typing
    function getCurrentWord(text) {
        // Remove only trailing whitespace so we can detect when the user ended a word
        const trimmedEnd = text.trimEnd();

        // If nothing meaningful is left, return empty string
        if (!trimmedEnd) return "";

        // Split on one or more whitespace characters
        const words = trimmedEnd.split(/\s+/);

        // Return the last word, or empty string if somehow missing
        return words[words.length - 1] || "";
    }


    // EVENT: Runs every time the user types in the textarea
    descriptionInput.addEventListener("input", () => {

        // Cancel any previous delayed API call
        clearTimeout(debounceTimer);

        // Get the full text from the textarea
        const fullText = descriptionInput.value;

        // Get only the word currently being typed
        const currentWord = getCurrentWord(fullText);

        // Only show suggestions when the CURRENT WORD has at least 2 characters
        // This prevents cases like "p a" from triggering suggestions for "a"
        if (currentWord.length < 2) {
            hideSuggestions();
            return;
        }

        // Wait 200ms before calling the API
        debounceTimer = setTimeout(async () => {
            try {
                // Send only the current word to the backend
                const response = await fetch(`/api/reportassist/suggestions?q=${encodeURIComponent(currentWord)}`);

                // If the API failed, hide suggestions
                if (!response.ok) {
                    hideSuggestions();
                    return;
                }

                // Convert the JSON response into a JS array
                suggestions = await response.json();

                // Reset keyboard selection
                activeIndex = -1;

                // Display suggestions in the dropdown
                renderSuggestions(suggestions);

            } catch {
                // If any error happens, hide suggestions
                hideSuggestions();
            }
        }, 200);
    });


    // EVENT: Handles keyboard navigation inside the textarea
    descriptionInput.addEventListener("keydown", (e) => {

        // If there are no suggestions, do nothing
        if (suggestions.length === 0) return;

        // ArrowDown -> move selection down
        if (e.key === "ArrowDown") {
            e.preventDefault();

            // Move down but don't go past the last item
            activeIndex = Math.min(activeIndex + 1, suggestions.length - 1);
            renderSuggestions(suggestions);

        // ArrowUp -> move selection up
        } else if (e.key === "ArrowUp") {
            e.preventDefault();

            // Move up but don't go below 0
            activeIndex = Math.max(activeIndex - 1, 0);
            renderSuggestions(suggestions);

        // Enter -> accept selected suggestion
        } else if (e.key === "Enter") {
            if (activeIndex >= 0) {
                e.preventDefault();
                applySuggestion(suggestions[activeIndex]);
            }

        // Escape -> close dropdown
        } else if (e.key === "Escape") {
            hideSuggestions();
        }
    });


    // EVENT: Click anywhere on the page
    document.addEventListener("click", (e) => {

        // If click is outside the suggestions box and not the textarea, hide suggestions
        if (!suggestionsBox.contains(e.target) && e.target !== descriptionInput) {
            hideSuggestions();
        }
    });


    // FUNCTION: Render suggestions into the dropdown
    function renderSuggestions(items) {

        // Clear any existing suggestions
        suggestionsBox.innerHTML = "";

        // If no suggestions, hide dropdown
        if (!items || items.length === 0) {
            hideSuggestions();
            return;
        }

        // Loop through each suggestion
        items.forEach((item, index) => {

            // Create a div for each suggestion
            const div = document.createElement("div");

            // Add styling class + highlight if active
            div.className = "autocomplete-item" + (index === activeIndex ? " active" : "");

            // Set the visible text
            div.textContent = item;

            // Use mousedown instead of click so the textarea doesn't lose focus first
            div.addEventListener("mousedown", (e) => {
                e.preventDefault();
                applySuggestion(item);
            });

            // Add this suggestion to the dropdown
            suggestionsBox.appendChild(div);
        });

        // Show the dropdown
        suggestionsBox.classList.remove("d-none");
    }


    // FUNCTION: Apply a selected suggestion to the textarea
    function applySuggestion(suggestion) {

        // Get the full current text
        const currentText = descriptionInput.value;

        // Remove trailing spaces for cleaner last-word replacement logic
        const trimmed = currentText.trimEnd();

        // Find where the last word starts
        const lastSpaceIndex = trimmed.lastIndexOf(" ");

        // Keep everything before the last word
        const prefix = lastSpaceIndex >= 0
            ? trimmed.substring(0, lastSpaceIndex + 1)
            : "";

        // Replace only the current word with the chosen suggestion
        descriptionInput.value = prefix + suggestion;

        // Fire input again so any word count / character count logic updates
        descriptionInput.dispatchEvent(new Event("input"));

        // Hide dropdown after selection
        hideSuggestions();

        // Keep focus in textarea
        descriptionInput.focus();
    }


    // FUNCTION: Hide and reset suggestions
    function hideSuggestions() {

        // Clear suggestions array
        suggestions = [];

        // Reset keyboard selection
        activeIndex = -1;

        // Remove all suggestion elements
        suggestionsBox.innerHTML = "";

        // Hide dropdown visually
        suggestionsBox.classList.add("d-none");
    }
});