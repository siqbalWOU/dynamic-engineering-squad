// SCRUM113

const {
    buildRecentReportDetailsUrl,
    navigateToRecentReport
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