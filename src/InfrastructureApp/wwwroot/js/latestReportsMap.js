// SCRUM-98:
// Loads location data for the selected report and displays it
// on a Google Map inside the Latest Reports modal.

document.addEventListener("DOMContentLoaded", function () {

    // Modal + map elements used in the popup
    const reportModal = document.getElementById("reportModal");
    const reportItems = document.querySelectorAll(".report-item");
    const modalMapElement = document.getElementById("modalReportMap");
    const modalMapFallbackElement = document.getElementById("modalMapFallback");

    // Stop if required elements are not on the page
    if (!reportModal || !modalMapElement || !modalMapFallbackElement) {
        return;
    }

    // Stores coordinates until the modal finishes opening
    let pendingLatitude = null;
    let pendingLongitude = null;

    // When a report item is clicked, request its location from the API
    reportItems.forEach(function (item) {
        item.addEventListener("click", async function () {

            const reportId = this.dataset.reportid || "";

            // Reset map state before loading new report data
            pendingLatitude = null;
            pendingLongitude = null;
            modalMapElement.style.display = "none";
            modalMapElement.innerHTML = "";
            modalMapFallbackElement.classList.add("d-none");
            modalMapFallbackElement.textContent = "";

            if (!reportId) {
                return;
            }

            try {
                // Call API endpoint to load report details
                const response = await fetch(`/api/reports/${reportId}`);

                if (!response.ok) {
                    showMapFallback("Location could not be loaded.");
                    return;
                }

                const report = await response.json();

                // Save coordinates for rendering when modal is visible
                pendingLatitude = report.latitude;
                pendingLongitude = report.longitude;
            }
            catch (error) {
                console.error("Error loading report location:", error);
                showMapFallback("Location could not be loaded.");
            }
        });
    });

    // When the modal finishes opening, render the map
    reportModal.addEventListener("shown.bs.modal", function () {
        renderMap(pendingLatitude, pendingLongitude);
    });

    // Creates the Google Map and marker
    function renderMap(latitude, longitude) {

        // If coordinates are missing, show fallback message
        if (latitude == null || longitude == null || isNaN(latitude) || isNaN(longitude)) {
            showMapFallback("Location is not available for this report.");
            return;
        }

        // Ensure Google Maps API loaded correctly
        if (typeof google === "undefined" || !google.maps) {
            showMapFallback("Map could not be loaded.");
            return;
        }

        modalMapFallbackElement.classList.add("d-none");
        modalMapFallbackElement.textContent = "";

        modalMapElement.style.display = "block";
        modalMapElement.innerHTML = "";

        const position = {
            lat: parseFloat(latitude),
            lng: parseFloat(longitude)
        };

        // Initialize map centered on report location
        const map = new google.maps.Map(modalMapElement, {
            center: position,
            zoom: 15
        });

        // Place marker at report location
        new google.maps.Marker({
            position: position,
            map: map
        });
    }

    // Shows message when map cannot be displayed
    function showMapFallback(message) {
        modalMapElement.style.display = "none";
        modalMapFallbackElement.textContent = message;
        modalMapFallbackElement.classList.remove("d-none");
    }
});