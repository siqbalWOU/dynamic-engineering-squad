// SCRUM113

const {
    buildRecentReportDetailsUrl,
    navigateToRecentReport,
    isRecentReportInteractiveElement,
    toggleRecentReportDetails,
    setRecentReportExpanded
} = require("../InfrastructureApp/wwwroot/js/homeRecentReports");

describe("SCRUM-113 homeRecentReports.js", () => {

    // -------------------------------------------------------
    // TEST 1: Build correct details URL
    // -------------------------------------------------------
    test("buildRecentReportDetailsUrl returns correct details path", () => {
        const result = buildRecentReportDetailsUrl("25");

        expect(result).toBe("/ReportIssue/Details/25");
    });

    // -------------------------------------------------------
    // TEST 2: Return empty string when ID is missing
    // -------------------------------------------------------
    test("buildRecentReportDetailsUrl returns empty string when report id is missing", () => {
        expect(buildRecentReportDetailsUrl("")).toBe("");
        expect(buildRecentReportDetailsUrl(null)).toBe("");
        expect(buildRecentReportDetailsUrl(undefined)).toBe("");
    });

    // -------------------------------------------------------
    // TEST 3: Navigation happens when valid report ID exists
    // -------------------------------------------------------
    test("navigateToRecentReport redirects when report id exists", () => {
        const item = {
            dataset: {
                reportid: "42"
            }
        };

        const fakeNavigate = jest.fn();

        navigateToRecentReport(item, fakeNavigate);

        expect(fakeNavigate).toHaveBeenCalledWith("/ReportIssue/Details/42");
    });

    // -------------------------------------------------------
    // TEST 4: No navigation when item is null
    // -------------------------------------------------------
    test("navigateToRecentReport does nothing when item is missing", () => {
        const fakeNavigate = jest.fn();

        navigateToRecentReport(null, fakeNavigate);

        expect(fakeNavigate).not.toHaveBeenCalled();
    });

    // -------------------------------------------------------
    // TEST 5: No navigation when report ID is empty
    // -------------------------------------------------------
    test("navigateToRecentReport does nothing when report id is missing", () => {
        const item = {
            dataset: {
                reportid: ""
            }
        };

        const fakeNavigate = jest.fn();

        navigateToRecentReport(item, fakeNavigate);

        expect(fakeNavigate).not.toHaveBeenCalled();
    });
});

// SCRUM-128:
// Tests for Home page recent report inline expand/collapse behavior
describe("SCRUM-128 homeRecentReports.js", () => {

    beforeEach(() => {
        document.body.innerHTML = `
            <div id="recentReportsList">
                <div class="home-report-item" data-reportid="1">
                    <button type="button" class="home-report-toggle" aria-expanded="false">Expand</button>
                    <div class="home-report-details" hidden>Report 1 details</div>
                </div>
                <div class="home-report-item" data-reportid="2">
                    <button type="button" class="home-report-toggle" aria-expanded="false">Expand</button>
                    <div class="home-report-details" hidden>Report 2 details</div>
                </div>
            </div>
        `;
    });

    // -------------------------------------------------------
    // TEST 6: Expanding a report shows its inline details
    // -------------------------------------------------------
    test("toggleRecentReportDetails expands hidden report details", () => {
        const item = document.querySelector(".home-report-item");
        const detailsPanel = item.querySelector(".home-report-details");
        const toggleButton = item.querySelector(".home-report-toggle");

        toggleRecentReportDetails(item);

        expect(detailsPanel.hidden).toBe(false);
        expect(toggleButton.getAttribute("aria-expanded")).toBe("true");
        expect(toggleButton.textContent).toBe("Collapse");
    });

    // -------------------------------------------------------
    // TEST 7: Collapsing a report hides its inline details
    // -------------------------------------------------------
    test("toggleRecentReportDetails collapses visible report details", () => {
        const item = document.querySelector(".home-report-item");
        const detailsPanel = item.querySelector(".home-report-details");
        const toggleButton = item.querySelector(".home-report-toggle");

        setRecentReportExpanded(item, true);
        toggleRecentReportDetails(item);

        expect(detailsPanel.hidden).toBe(true);
        expect(toggleButton.getAttribute("aria-expanded")).toBe("false");
        expect(toggleButton.textContent).toBe("Expand");
    });

    // -------------------------------------------------------
    // TEST 8: Only one Home recent report can be expanded
    // -------------------------------------------------------
    test("toggleRecentReportDetails collapses other expanded reports", () => {
        const items = document.querySelectorAll(".home-report-item");
        const firstItem = items[0];
        const secondItem = items[1];

        toggleRecentReportDetails(firstItem);
        toggleRecentReportDetails(secondItem);

        expect(firstItem.querySelector(".home-report-details").hidden).toBe(true);
        expect(firstItem.querySelector(".home-report-toggle").getAttribute("aria-expanded")).toBe("false");
        expect(secondItem.querySelector(".home-report-details").hidden).toBe(false);
        expect(secondItem.querySelector(".home-report-toggle").getAttribute("aria-expanded")).toBe("true");
    });

    // -------------------------------------------------------
    // TEST 9: Helper recognizes buttons and links as interactive elements
    // -------------------------------------------------------
    test("isRecentReportInteractiveElement detects buttons and links", () => {
        const button = document.querySelector(".home-report-toggle");
        const link = document.createElement("a");
        const plainText = document.createElement("span");

        expect(isRecentReportInteractiveElement(button)).toBe(true);
        expect(isRecentReportInteractiveElement(link)).toBe(true);
        expect(isRecentReportInteractiveElement(plainText)).toBe(false);
        expect(isRecentReportInteractiveElement(null)).toBe(false);
    });

    // -------------------------------------------------------
    // TEST 10: Missing details panel does not throw an error
    // -------------------------------------------------------
    test("toggleRecentReportDetails handles missing details panel", () => {
        const item = document.createElement("div");
        item.className = "home-report-item";

        expect(() => toggleRecentReportDetails(item)).not.toThrow();
    });
});
