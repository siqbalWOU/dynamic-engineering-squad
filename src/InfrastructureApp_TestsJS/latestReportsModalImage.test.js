// JS tests for Latest Reports modal image fallback behavior

// Import helper functions from the modal JavaScript file
// These functions handle image fallback behavior inside the modal
const {
    showMissingImageFallback,
    showBrokenImageFallback
} = require("../InfrastructureApp/wwwroot/js/latestReportsModal.js");

// Group of tests for modal image fallback behavior
describe("Latest Reports Modal Image Fallback Tests", () => {

    let image;
    let fallback;

    // Setup step that runs before each test
    // Creates a simulated DOM environment for the modal image and fallback elements
    beforeEach(() => {
        document.body.innerHTML = `
            <img id="modalImage" src="/uploads/test.jpg" />
            <div id="modalImageFallback" class="d-none"></div>
        `;

        // Retrieve the simulated DOM elements
        image = document.getElementById("modalImage");
        fallback = document.getElementById("modalImageFallback");
    });

    // Test 1
    // Verify that the fallback message is shown when a report has no image URL
    test("shows fallback when no image URL exists", () => {

        // Call the function that should hide the image and display the fallback message
        showMissingImageFallback(image, fallback);

        // Confirm the image element is hidden
        expect(image.classList.contains("d-none")).toBe(true);

        // Confirm the fallback message becomes visible
        expect(fallback.classList.contains("d-none")).toBe(false);

        // Confirm the fallback message text indicates that no image was provided
        expect(fallback.innerHTML).toContain("No image");
    });

    // Test 2
    // Verify that the fallback message is shown when the image fails to load
    test("shows fallback when image fails to load", () => {

        // Call the function that should handle broken image behavior
        showBrokenImageFallback(image, fallback);

        // Confirm the image element is hidden
        expect(image.classList.contains("d-none")).toBe(true);

        // Confirm the fallback message becomes visible
        expect(fallback.classList.contains("d-none")).toBe(false);

        // Confirm the fallback message explains that the image could not load
        expect(fallback.innerHTML).toContain("could not be loaded");
    });

});