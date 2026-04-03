// Feature-81: Latest Reports modal population 
document.addEventListener("DOMContentLoaded", function () {

    // Only run on pages that contain the modal
    const reportModal = document.getElementById("reportModal");
    if (!reportModal) {
        return;
    }

    // Modal content elements
    const modalDescriptionElement = document.getElementById("modalDescription");
    const modalCreatedElement = document.getElementById("modalCreated");
    const modalStatusElement = document.getElementById("modalStatus");

    // Modal image elements
    const modalImageElement = document.getElementById("modalImage");
    const modalImageFallbackElement = document.getElementById("modalImageFallback");

    // -------------------------------------------------------
    // SCRUM-101 ADDED
    // URL link that opens the existing ReportIssue details page
    // Example: /ReportIssue/Details/5
    // -------------------------------------------------------
    const openFullReportLink = document.getElementById("openFullReportLink");

    // -------------------------------------------------------
    // SCRUM-101 / pushState EXPERIMENT ADDED
    // Keep track of the Latest Reports page URL so it can be
    // restored after the modal is closed.
    // This is the concept Chris suggested.
    // -------------------------------------------------------
    const latestReportsUrl = "/Reports/Latest";

    // -------------------------------------------------------
    // SCRUM-101 / pushState EXPERIMENT ADDED
    // Update the browser URL to match the selected report
    // without reloading the page.
    // -------------------------------------------------------
    function pushReportUrl(reportId) {
        if (!reportId) return;

        const newUrl = `/ReportIssue/Details/${reportId}`;

        // Avoid pushing duplicate history entries for the same URL
        if (window.location.pathname !== newUrl) {
            history.pushState({ reportId: reportId }, "", newUrl);
        }
    }

    // -------------------------------------------------------
    // SCRUM-101 / pushState EXPERIMENT ADDED
    // Restore browser URL back to the Latest Reports page
    // after the modal is closed.
    // -------------------------------------------------------
    function restoreLatestReportsUrl() {
        if (window.location.pathname !== latestReportsUrl) {
            history.replaceState({}, "", latestReportsUrl);
        }
    }

    // -------------------------------------------------------
    // SCRUM-101 / pushState EXPERIMENT ADDED
    // When the modal closes, restore the URL to /Reports/Latest
    // -------------------------------------------------------
    reportModal.addEventListener("hidden.bs.modal", function () {
        restoreLatestReportsUrl();
    });

    // -------------------------------------------------------
    // SCRUM-101 / pushState EXPERIMENT ADDED
    // If the user presses the browser Back button while the
    // modal is open, close the modal.
    // -------------------------------------------------------
    window.addEventListener("popstate", function () {
        const modalInstance = bootstrap.Modal.getInstance(reportModal);
        if (modalInstance) {
            modalInstance.hide();
        }
    });

    // Get all clickable report items
    const reportItems = document.querySelectorAll(".report-item");

    reportItems.forEach(function (item) {

        item.addEventListener("click", function () {

            const description = this.dataset.description || "";
            const created = this.dataset.created || "";
            const status = this.dataset.status || "";
            const imageUrl = this.dataset.image || ""; // read image URL from data-image attribute

            // -------------------------------------------------------
            // SCRUM-101 ADDED
            // Read report id so the modal can show a unique URL link
            // -------------------------------------------------------
            const reportId = this.dataset.reportid || "";

            // -------------------------------------------------------
            // SCRUM-101 ADDED
            // Point the modal link to Sunair's existing details page
            // -------------------------------------------------------
            if (openFullReportLink && reportId) {
                openFullReportLink.href = `/ReportIssue/Details/${reportId}`;
            }

            // -------------------------------------------------------
            // SCRUM-101 / pushState EXPERIMENT ADDED
            // Update the browser URL when the modal opens so the
            // selected report has a contextual deep link.
            // -------------------------------------------------------
            pushReportUrl(reportId);

            if (modalDescriptionElement) {
                modalDescriptionElement.textContent = description;
            }

            if (modalCreatedElement) {
                modalCreatedElement.textContent = created;
            }

            if (modalStatusElement) {
                modalStatusElement.textContent = status;
            }

            // Image handling logic 
            if (modalImageElement && modalImageFallbackElement) {

                //SCRUM-102 ADDED
                //Clear any old error handler before setting a new image
                modalImageElement.onerror = null;

                if (imageUrl.trim().length > 0) {

                    //SCRUM-102 ADDDED
                    //If the image file fails to load, hide the broken image
                    // and show a clear fallback message instead.
                    modalImageElement.onerror = function(){
                        modalImageElement.removeAttribute("src")
                        modalImageElement.classList.add("d-none");

                        modalImageFallbackElement.innerHTML = "<strong>⚠ Image could not be loaded.</strong> The image file may be missing.";
                        modalImageFallbackElement.classList.remove("d-none");
                    }

                    // Show image if URL exists
                    modalImageElement.src = imageUrl;
                    modalImageElement.classList.remove("d-none");

                    modalImageFallbackElement.classList.add("d-none");
                    modalImageFallbackElement.innerHTML = "";
                }
                else {
                    // Show fallback message if no image
                    modalImageElement.removeAttribute("src");
                    modalImageElement.classList.add("d-none");

                    modalImageFallbackElement.innerHTML = "<strong>📷 No image available.</strong> This report was submitted without an image.";
                    modalImageFallbackElement.classList.remove("d-none");
                }
            }
        });
    });
});

// SCRUM-101 test helper
// Sets the modal link to the correct report details page
function setReportDetailsLink(linkElement, reportId) {

    // Only update the link if both values exist
    if (linkElement && reportId) {

        // Create the URL for the report details page
        linkElement.href = `/ReportIssue/Details/${reportId}`;
    }
}

// SCRUM-101 test helper
// Stores the URL for the Latest Reports page
const latestReportsUrl = "/Reports/Latest";

// SCRUM-101 test helper
// Updates the browser URL when a report is opened in the modal
function pushReportUrl(reportId) {

    // Do nothing if reportId is missing
    if (!reportId) return;

    // Build the new report details URL
    const newUrl = `/ReportIssue/Details/${reportId}`;

    // Avoid pushing the same URL multiple times
    if (window.location.pathname !== newUrl) {

        // Update the browser history without reloading the page
        history.pushState({ reportId: reportId }, "", newUrl);
    }
}

// SCRUM-101 test helper
// Restores the browser URL back to the Latest Reports page
function restoreLatestReportsUrl() {

    // Only change the URL if it is not already Latest Reports
    if (window.location.pathname !== latestReportsUrl) {

        // Replace the current history entry with the Latest Reports URL
        history.replaceState({}, "", latestReportsUrl);
    }
}


// -------------------------------------------------------
// SCRUM-102 test helpers
// These functions allow Jest tests to verify image fallback behavior
// -------------------------------------------------------

// Shows fallback when a report has no image URL
function showMissingImageFallback(modalImageElement, modalImageFallbackElement) {

    modalImageElement.removeAttribute("src");
    modalImageElement.classList.add("d-none");

    modalImageFallbackElement.innerHTML =
        "<strong>📷 No image available.</strong> This report was submitted without an image.";

    modalImageFallbackElement.classList.remove("d-none");
}

// Shows fallback when an image file fails to load
function showBrokenImageFallback(modalImageElement, modalImageFallbackElement) {

    modalImageElement.removeAttribute("src");
    modalImageElement.classList.add("d-none");

    modalImageFallbackElement.innerHTML =
        "<strong>⚠ Image could not be loaded.</strong> The image file may be missing.";

    modalImageFallbackElement.classList.remove("d-none");
}



// SCRUM-101 / SCRUM-102 test exports
// Allows Jest tests to import helper functions
if (typeof module !== "undefined") {
    module.exports = {
        setReportDetailsLink,
        pushReportUrl,
        restoreLatestReportsUrl,
        showMissingImageFallback,
        showBrokenImageFallback
    };
}
