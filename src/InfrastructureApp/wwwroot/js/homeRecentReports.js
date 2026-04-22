// This is SCRUM 113

document.addEventListener("DOMContentLoaded", function () {
    const reportItems = document.querySelectorAll(".home-report-item");

    // Stop if there are no recent report items on the page
    if (!reportItems.length) {
        return;
    }

    reportItems.forEach(function (item) {

        // Navigate when the user clicks a recent report row
        item.addEventListener("click", function () {
            navigateToRecentReport(item);
        });

        // SCRUM-113:
        // Support keyboard navigation for accessibility
        item.addEventListener("keydown", function (event) {
            if (event.key === "Enter" || event.key === " ") {
                event.preventDefault();
                navigateToRecentReport(item);
            }
        });
    });
});

// SCRUM-113:
// Build the details page URL using the report id
function buildRecentReportDetailsUrl(reportId) {
    if (!reportId) {
        return "";
    }

    return `/ReportIssue/Details/${reportId}`;
}

// SCRUM-113:
// Navigate to the existing details page for the selected report
function navigateToRecentReport(item, navigateFn = window.location.assign.bind(window.location)) {
    if (!item) {
        return;
    }

    const reportId = item.dataset.reportid || "";
    const detailsUrl = buildRecentReportDetailsUrl(reportId);

    if (!detailsUrl) {
        return;
    }

    // SCRUM-113:
    // Use a navigation function so JS tests can pass in a fake redirect
    navigateFn(detailsUrl);
}

// SCRUM-113:
// Export helpers for JavaScript unit tests
if (typeof module !== "undefined") {
    module.exports = {
        buildRecentReportDetailsUrl,
        navigateToRecentReport
    };
}