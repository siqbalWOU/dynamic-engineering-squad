// This is SCRUM 113

document.addEventListener("DOMContentLoaded", function () {
    const reportItems = document.querySelectorAll(".home-report-item");

    // Stop if there are no recent report items on the page
    if (!reportItems.length) {
        return;
    }

    reportItems.forEach(function (item) {
        // SCRUM-128:
        // Find the inline expand/collapse button for this Home page report row
        const toggleButton = item.querySelector(".home-report-toggle");

        if (toggleButton) {
            // SCRUM-128:
            // Toggle inline details without triggering existing View Details navigation
            toggleButton.addEventListener("click", function (event) {
                event.preventDefault();
                event.stopPropagation();
                toggleRecentReportDetails(item);
            });
        }

        // Navigate when the user clicks a recent report row
        item.addEventListener("click", function (event) {
            if (isRecentReportInteractiveElement(event.target)) {
                return;
            }

            navigateToRecentReport(item);
        });

        // SCRUM-113:
        // Support keyboard navigation for accessibility
        item.addEventListener("keydown", function (event) {
            if (isRecentReportInteractiveElement(event.target)) {
                return;
            }

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

// SCRUM-128:
// Keep row navigation separate from inline expand/collapse controls
function isRecentReportInteractiveElement(target) {
    if (!target || !target.closest) {
        return false;
    }

    return !!target.closest("button, a");
}

// SCRUM-128:
// Show or hide one Home page report detail panel at a time
function toggleRecentReportDetails(item) {
    if (!item) {
        return;
    }

    const detailsPanel = item.querySelector(".home-report-details");

    if (!detailsPanel) {
        return;
    }

    const shouldExpand = detailsPanel.hidden;

    if (shouldExpand) {
        collapseOtherRecentReportDetails(item);
        setRecentReportExpanded(item, true);
        return;
    }

    setRecentReportExpanded(item, false);
}

// SCRUM-128:
// Collapse any other expanded report in the same Home recent reports list
function collapseOtherRecentReportDetails(activeItem) {
    const reportList = activeItem.closest("#recentReportsList");

    if (!reportList) {
        return;
    }

    const expandedItems = reportList.querySelectorAll(".home-report-item");

    expandedItems.forEach(function (item) {
        if (item !== activeItem) {
            setRecentReportExpanded(item, false);
        }
    });
}

// SCRUM-128:
// Update panel visibility and button accessibility state together
function setRecentReportExpanded(item, isExpanded) {
    const detailsPanel = item.querySelector(".home-report-details");
    const toggleButton = item.querySelector(".home-report-toggle");

    if (!detailsPanel) {
        return;
    }

    detailsPanel.hidden = !isExpanded;

    if (toggleButton) {
        toggleButton.setAttribute("aria-expanded", isExpanded ? "true" : "false");
        toggleButton.textContent = isExpanded ? "Collapse" : "Expand";
    }
}

// SCRUM-113:
// Export helpers for JavaScript unit tests
if (typeof module !== "undefined") {
    module.exports = {
        buildRecentReportDetailsUrl,
        navigateToRecentReport,
        isRecentReportInteractiveElement,
        toggleRecentReportDetails,
        setRecentReportExpanded
    };
}
