/**
 * Tell Jest to use the browser-like jsdom environment
 * so document, window, textarea, div, events, etc. all exist.
 */
describe("reportAssist.js", () => {

    // These variables will point to DOM elements created fresh for each test
    let descriptionInput;
    let suggestionsBox;

    /**
     * Helper function:
     * Loads the real JS file we want to test.
     *
     * This executes reportAssist.js so its event listeners get registered.
     */
    function loadScript() {
        require("../InfrastructureApp/wwwroot/js/reportAssist.js");
    }

    /**
     * Runs before every single test.
     *
     * Purpose:
     * - reset module state so the JS file is reloaded fresh each time
     * - use fake timers so we can control debounce delays manually
     * - create a fake HTML page for the script to interact with
     * - mock fetch so no real API calls happen
     * - force DOMContentLoaded logic to run immediately
     */
    beforeEach(() => {
        // Clears previously cached required modules
        // so reportAssist.js reloads fresh for each test
        jest.resetModules();

        // Lets us manually control setTimeout/setInterval behavior
        // This is important because the autocomplete uses a 200ms debounce
        jest.useFakeTimers();

        // Build the minimal fake HTML the script expects:
        // - textarea with id="Description"
        // - dropdown container with id="descriptionSuggestions"
        document.body.innerHTML = `
            <textarea id="Description"></textarea>
            <div id="descriptionSuggestions" class="d-none"></div>
        `;

        // Grab references to those fake DOM elements
        descriptionInput = document.getElementById("Description");
        suggestionsBox = document.getElementById("descriptionSuggestions");

        // Replace real fetch with a Jest mock function
        // so we can control what the backend "returns"
        global.fetch = jest.fn();

        // Save the original addEventListener so we can still use it
        // for all other events besides DOMContentLoaded
        const originalAddEventListener = document.addEventListener.bind(document);

        /**
         * Spy on document.addEventListener.
         *
         * Why?
         * reportAssist.js waits for DOMContentLoaded before setting itself up.
         * In Jest, we want that callback to run immediately during the test.
         *
         * So when reportAssist.js says:
         * document.addEventListener("DOMContentLoaded", callback)
         *
         * we immediately invoke callback ourselves.
         */
        jest.spyOn(document, "addEventListener").mockImplementation((type, listener, options) => {
            if (type === "DOMContentLoaded") {
                listener(new Event("DOMContentLoaded"));
                return;
            }

            // For all other event types, use the real addEventListener
            return originalAddEventListener(type, listener, options);
        });

        // Finally load the file under test
        loadScript();
    });

    /**
     * Runs after every test.
     *
     * Purpose:
     * - clear mock call history
     * - clear fake timers
     * - restore real timers
     * - restore any mocked/spied functions
     */
    afterEach(() => {
        jest.clearAllMocks();
        jest.clearAllTimers();
        jest.useRealTimers();
        jest.restoreAllMocks();
    });

    /**
     * Test:
     * If user types fewer than 2 characters,
     * the script should not call the backend
     * and should keep suggestions hidden.
     */
    test("does nothing if query length is less than 2", () => {
        // Simulate typing one character
        descriptionInput.value = "a";
        descriptionInput.dispatchEvent(new Event("input", { bubbles: true }));

        // Advance fake time beyond debounce window
        jest.advanceTimersByTime(250);

        // Backend should NOT be called
        expect(fetch).not.toHaveBeenCalled();

        // Suggestions box should remain empty and hidden
        expect(suggestionsBox.innerHTML).toBe("");
        expect(suggestionsBox.classList.contains("d-none")).toBe(true);
    });

    /**
     * Test:
     * When user types at least 2 characters,
     * after the debounce delay the script should:
     * - call the API
     * - render returned suggestions
     * - show the dropdown
     */
    test("calls API after debounce and renders suggestions", async () => {
        // Fake successful API response
        fetch.mockResolvedValue({
            ok: true,
            json: async () => ["pothole", "pole damage"]
        });

        // Simulate typing "po"
        descriptionInput.value = "po";
        descriptionInput.dispatchEvent(new Event("input", { bubbles: true }));

        // Fast-forward through the 200ms debounce delay
        jest.advanceTimersByTime(200);

        // Allow async promises (fetch + json) to finish
        await Promise.resolve();
        await Promise.resolve();

        // Expect exactly one API call with correct query string
        expect(fetch).toHaveBeenCalledTimes(1);
        expect(fetch).toHaveBeenCalledWith("/api/reportassist/suggestions?q=po");

        // Verify suggestions were rendered into the dropdown
        const items = suggestionsBox.querySelectorAll(".autocomplete-item");
        expect(items.length).toBe(2);
        expect(items[0].textContent).toBe("pothole");
        expect(items[1].textContent).toBe("pole damage");

        // Dropdown should be visible
        expect(suggestionsBox.classList.contains("d-none")).toBe(false);
    });

    /**
     * Test:
     * Debounce should prevent multiple rapid keystrokes
     * from causing multiple backend requests.
     *
     * Only the final input should result in one API call.
     */
    test("debounce prevents multiple rapid API calls", async () => {
        fetch.mockResolvedValue({
            ok: true,
            json: async () => ["pothole"]
        });

        // Simulate user typing quickly: p -> po -> pot
        descriptionInput.value = "p";
        descriptionInput.dispatchEvent(new Event("input", { bubbles: true }));

        descriptionInput.value = "po";
        descriptionInput.dispatchEvent(new Event("input", { bubbles: true }));

        descriptionInput.value = "pot";
        descriptionInput.dispatchEvent(new Event("input", { bubbles: true }));

        // Only the last debounced call should actually run
        jest.advanceTimersByTime(200);

        await Promise.resolve();
        await Promise.resolve();

        expect(fetch).toHaveBeenCalledTimes(1);
        expect(fetch).toHaveBeenCalledWith("/api/reportassist/suggestions?q=pot");
    });

    /**
     * Test:
     * If the backend responds with ok: false,
     * the script should hide suggestions instead of showing bad data.
     */
    test("hides suggestions if API response is not ok", async () => {
        fetch.mockResolvedValue({
            ok: false
        });

        descriptionInput.value = "po";
        descriptionInput.dispatchEvent(new Event("input", { bubbles: true }));

        jest.advanceTimersByTime(200);

        await Promise.resolve();

        expect(suggestionsBox.innerHTML).toBe("");
        expect(suggestionsBox.classList.contains("d-none")).toBe(true);
    });

    /**
     * Test:
     * If fetch throws an error (network error, server unavailable, etc.),
     * suggestions should also be hidden.
     */
    test("hides suggestions if fetch throws an error", async () => {
        fetch.mockRejectedValue(new Error("network error"));

        descriptionInput.value = "po";
        descriptionInput.dispatchEvent(new Event("input", { bubbles: true }));

        jest.advanceTimersByTime(200);

        await Promise.resolve();

        expect(suggestionsBox.innerHTML).toBe("");
        expect(suggestionsBox.classList.contains("d-none")).toBe(true);
    });

    /**
     * Test:
     * Pressing ArrowDown should move keyboard selection
     * to the first suggestion in the list.
     */
    test("ArrowDown highlights the first suggestion", async () => {
        fetch.mockResolvedValue({
            ok: true,
            json: async () => ["pothole", "pole damage"]
        });

        descriptionInput.value = "po";
        descriptionInput.dispatchEvent(new Event("input", { bubbles: true }));

        jest.advanceTimersByTime(200);
        await Promise.resolve();
        await Promise.resolve();

        // Simulate pressing ArrowDown
        descriptionInput.dispatchEvent(
            new KeyboardEvent("keydown", { key: "ArrowDown", bubbles: true })
        );

        // First item should now be highlighted
        const items = suggestionsBox.querySelectorAll(".autocomplete-item");
        expect(items[0].classList.contains("active")).toBe(true);
        expect(items[1].classList.contains("active")).toBe(false);
    });

    /**
     * Test:
     * ArrowDown twice then ArrowUp should move selection down and back up.
     * Final result should be that the first item is highlighted again.
     */
    test("ArrowDown then ArrowUp updates highlighted suggestion", async () => {
        fetch.mockResolvedValue({
            ok: true,
            json: async () => ["pothole", "pole damage"]
        });

        descriptionInput.value = "po";
        descriptionInput.dispatchEvent(new Event("input", { bubbles: true }));

        jest.advanceTimersByTime(200);
        await Promise.resolve();
        await Promise.resolve();

        descriptionInput.dispatchEvent(
            new KeyboardEvent("keydown", { key: "ArrowDown", bubbles: true })
        );
        descriptionInput.dispatchEvent(
            new KeyboardEvent("keydown", { key: "ArrowDown", bubbles: true })
        );
        descriptionInput.dispatchEvent(
            new KeyboardEvent("keydown", { key: "ArrowUp", bubbles: true })
        );

        const items = suggestionsBox.querySelectorAll(".autocomplete-item");
        expect(items[0].classList.contains("active")).toBe(true);
        expect(items[1].classList.contains("active")).toBe(false);
    });

    /**
     * Test:
     * Pressing Enter on a highlighted suggestion should:
     * - apply the selected suggestion
     * - replace only the last word
     * - hide the dropdown
     *
     * Example:
     * "big pot" -> "big pothole"
     */
    test("Enter applies the active suggestion and replaces only the last word", async () => {
        fetch.mockResolvedValue({
            ok: true,
            json: async () => ["pothole"]
        });

        descriptionInput.value = "big pot";
        descriptionInput.dispatchEvent(new Event("input", { bubbles: true }));

        jest.advanceTimersByTime(200);
        await Promise.resolve();
        await Promise.resolve();

        // Highlight the first suggestion
        descriptionInput.dispatchEvent(
            new KeyboardEvent("keydown", { key: "ArrowDown", bubbles: true })
        );

        // Press Enter to apply it
        descriptionInput.dispatchEvent(
            new KeyboardEvent("keydown", { key: "Enter", bubbles: true })
        );

        expect(descriptionInput.value).toBe("big pothole");
        expect(suggestionsBox.innerHTML).toBe("");
        expect(suggestionsBox.classList.contains("d-none")).toBe(true);
    });

    /**
     * Test:
     * Clicking a suggestion with the mouse should apply it
     * the same way keyboard selection does.
     */
    test("mousedown on suggestion applies it", async () => {
        fetch.mockResolvedValue({
            ok: true,
            json: async () => ["streetlight"]
        });

        descriptionInput.value = "broken stre";
        descriptionInput.dispatchEvent(new Event("input", { bubbles: true }));

        jest.advanceTimersByTime(200);
        await Promise.resolve();
        await Promise.resolve();

        // Grab the rendered suggestion and simulate mouse click
        const item = suggestionsBox.querySelector(".autocomplete-item");
        item.dispatchEvent(new MouseEvent("mousedown", { bubbles: true }));

        expect(descriptionInput.value).toBe("broken streetlight");
        expect(suggestionsBox.classList.contains("d-none")).toBe(true);
    });

    /**
     * Test:
     * Pressing Escape should close and clear the dropdown.
     */
    test("Escape hides suggestions", async () => {
        fetch.mockResolvedValue({
            ok: true,
            json: async () => ["pothole"]
        });

        descriptionInput.value = "po";
        descriptionInput.dispatchEvent(new Event("input", { bubbles: true }));

        jest.advanceTimersByTime(200);
        await Promise.resolve();
        await Promise.resolve();

        descriptionInput.dispatchEvent(
            new KeyboardEvent("keydown", { key: "Escape", bubbles: true })
        );

        expect(suggestionsBox.innerHTML).toBe("");
        expect(suggestionsBox.classList.contains("d-none")).toBe(true);
    });

    /**
     * Test:
     * Clicking somewhere outside the textarea and dropdown
     * should close the suggestions list.
     */
    test("clicking outside hides suggestions", async () => {
        fetch.mockResolvedValue({
            ok: true,
            json: async () => ["pothole"]
        });

        descriptionInput.value = "po";
        descriptionInput.dispatchEvent(new Event("input", { bubbles: true }));

        jest.advanceTimersByTime(200);
        await Promise.resolve();
        await Promise.resolve();

        // Create a random outside element and click it
        const outside = document.createElement("div");
        document.body.appendChild(outside);

        outside.dispatchEvent(new MouseEvent("click", { bubbles: true }));

        expect(suggestionsBox.innerHTML).toBe("");
        expect(suggestionsBox.classList.contains("d-none")).toBe(true);
    });

    /**
     * Test:
     * Clicking inside the textarea should NOT close the dropdown.
     * The user should still be able to keep typing/selecting.
     */
    test("clicking inside textarea does not hide suggestions", async () => {
        fetch.mockResolvedValue({
            ok: true,
            json: async () => ["pothole"]
        });

        descriptionInput.value = "po";
        descriptionInput.dispatchEvent(new Event("input", { bubbles: true }));

        jest.advanceTimersByTime(200);
        await Promise.resolve();
        await Promise.resolve();

        // Click back into the textarea
        descriptionInput.dispatchEvent(new MouseEvent("click", { bubbles: true }));

        // Suggestions should still exist and still be visible
        expect(suggestionsBox.querySelectorAll(".autocomplete-item").length).toBe(1);
        expect(suggestionsBox.classList.contains("d-none")).toBe(false);
    });
});