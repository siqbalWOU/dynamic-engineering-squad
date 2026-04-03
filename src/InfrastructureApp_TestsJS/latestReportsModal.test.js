// SCRUM101: Testing 
const {
    pushReportUrl,
    restoreLatestReportsUrl,
    setReportDetailsLink
} = require("../InfrastructureApp/wwwroot/js/latestReportsModal.js");

describe("Latest Reports Modal JS Tests", () => {

    beforeEach(() => {
        document.body.innerHTML = `
            <a id="openFullReportLink" href="#"></a>
        `;

        // Reset URL before each test
        history.replaceState({}, "", "/Reports/Latest");
    });

    // -------------------------------------------------------
    // TEST 1: Verify details link is generated correctly
    // -------------------------------------------------------
    test("sets details link to the correct report URL", () => {

        // Get the link element used in the modal
        const link = document.getElementById("openFullReportLink");

        // Call helper to generate the report details link
        setReportDetailsLink(link, 5);

        // Verify the correct details URL is applied
        expect(link.href).toContain("/ReportIssue/Details/5");
    });

    // -------------------------------------------------------
    // TEST 2: Verify pushReportUrl updates browser URL
    // -------------------------------------------------------
    test("pushReportUrl updates browser URL for selected report", () => {

        // Simulate selecting report with id 7
        pushReportUrl(7);

        // Check that browser URL changed to report details
        expect(window.location.pathname).toBe("/ReportIssue/Details/7");
    });

    // -------------------------------------------------------
    // TEST 3: Verify URL is restored to Latest Reports
    // -------------------------------------------------------
    test("restoreLatestReportsUrl resets browser URL back to Latest Reports", () => {

        // Simulate currently being on a report details page
        history.replaceState({}, "", "/ReportIssue/Details/9");

        // Call function that restores the Latest Reports page URL
        restoreLatestReportsUrl();

        // Confirm URL is restored to Latest Reports
        expect(window.location.pathname).toBe("/Reports/Latest");
    });

    // -------------------------------------------------------
    // TEST 4: Verify link does nothing when reportId missing
    // -------------------------------------------------------
    test("setReportDetailsLink does nothing when reportId is missing", () => {

        // Get modal link element
        const link = document.getElementById("openFullReportLink");

        // Pass empty reportId
        setReportDetailsLink(link, "");

        // Link should remain unchanged
        expect(link.getAttribute("href")).toBe("#");
    });

    // -------------------------------------------------------
    // TEST 5: Verify pushReportUrl ignores empty reportId
    // -------------------------------------------------------
    test("pushReportUrl does nothing when reportId is missing", () => {

        // Call function with empty id
        pushReportUrl("");

        // URL should remain the same
        expect(window.location.pathname).toBe("/Reports/Latest");
    });
});