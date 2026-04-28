// ----------------------------------------------------
// Latest Reports Search (AJAX)
// This script handles searching the Latest Reports page
// without refreshing the page. It calls our internal
// REST API endpoint and dynamically updates the list.
// ----------------------------------------------------

// Feature-83: Latest Report Search
document.addEventListener("DOMContentLoaded", () => {

  // Get references to UI elements needed for search and results display
  const input = document.getElementById("latestSearchInput");
  const btn = document.getElementById("latestSearchButton");
  const clearBtn = document.getElementById("latestClearButton");
  const list = document.getElementById("latestReportsList");
  const msg = document.getElementById("latestSearchMessage");

  const sortSelect = document.getElementById("latestSortSelect"); // SCRUM-86 ADDED: reads sort dropdown value

  // if any required element is missing, stop execution
  // Prevents JavaScript errors if the page structure changes
  if (!input || !btn || !list || !msg)
    {
        return;
    }

  // ----------------------------------------------------
  // Performs the search using AJAX (fetch)
  // Calls our ReportsAPIController endpoint and updates
  // the Latest Reports list dynamically without page reload
  // ----------------------------------------------------
  async function runSearch(keyword, sort) { // SCRUM-86 UPDATED: accepts sort option

    // Clear any previous messages before running new search
    msg.innerHTML = "";

    // Build the REST API URL with encoded keyword parameter
    const url = `/api/reports/latest?query=${encodeURIComponent(keyword ?? "")}&sort=${encodeURIComponent(sort || "newest")}`; // SCRUM-86 UPDATED: include sort

    // Call the backend API
    const res = await fetch(url);

    // If server returns an error, show friendly message
    if (!res.ok) {
      msg.innerHTML = `<div class="alert alert-danger">Sorry, an error occurred. Try again.</div>`;
      return;
    }

    // Convert the JSON response into JavaScript objects
    const reports = await res.json();

    // If no matching reports found, clear list and show message
    if (!reports || reports.length === 0) {
      list.innerHTML = "";
      msg.innerHTML = `<div class="alert alert-info">No matching reports found.</div>`;
      return;
    }

    // rebuild list buttons (keep same data-* so modal still works)
    list.innerHTML = reports.map(r => {
    const created = new Date(r.createdAt);

      // FIX: consistent time format everywhere (no seconds)
      const createdShort = created.toLocaleString(undefined, {
        year: "numeric",
        month: "numeric",
        day: "numeric",
        hour: "numeric",
        minute: "2-digit"
    });

    return `
      <button type="button"
          class="list-group-item list-group-item-action d-flex justify-content-between align-items-start report-item"
          data-testid="latest-report-item"
          aria-controls="reportModal"
          onclick="window.openLatestReportModal?.(this)"
          data-report-id="${escapeHtml(String(r.id ?? ""))}"
          data-bs-toggle="modal"
          data-bs-target="#reportModal"
          data-reportid="${escapeHtml(String(r.id ?? ""))}"
          data-description="${escapeHtml(r.description ?? "")}"
          data-created="${escapeHtml(createdShort)}"
          data-status="${escapeHtml(r.status ?? "")}"
          data-image="${escapeHtml(r.imageUrl ?? "")}">
        <div class="me-3">
          <div class="fw-bold report-text-wrap text-break">${escapeHtml(r.description ?? "")}</div>
        </div>
        <small class="text-muted">${escapeHtml(createdShort)}</small>
      </button>
   `;
    }).join("");
  }

  // SCRUM-86 ADDED: shared loader so sort JS can reuse same fetch + render logic
  window.loadLatestReports = async function(keyword, sort) {
    await runSearch(keyword, sort);
  };

  btn.addEventListener("click", async () => {
    const keyword = input.value || "";
    const sort = sortSelect ? sortSelect.value : "newest"; // SCRUM-86 ADDED
    await runSearch(keyword, sort); // SCRUM-86 UPDATED
  });

  clearBtn.addEventListener("click", async () => {
    input.value = "";
    const sort = sortSelect ? sortSelect.value : "newest"; // SCRUM-86 ADDED
    await runSearch("", sort); // SCRUM-86 UPDATED
  });

  // Optional: press Enter to search
  input.addEventListener("keydown", async (e) => {
    if (e.key === "Enter") {
      e.preventDefault();
      const sort = sortSelect ? sortSelect.value : "newest"; // SCRUM-86 ADDED
      await runSearch(input.value || "", sort); // SCRUM-86 UPDATED
    }
  });



  // ----------------------------------------------------
  // This ensures modal works for dynamically added reports
  // ----------------------------------------------------
  document.addEventListener("click", (e) => {

    const item = e.target.closest(".report-item");
    if (!item)
    {
        return;
    } 
    const modalDescriptionElement = document.getElementById("modalDescription");
    const modalCreatedElement = document.getElementById("modalCreated");
    const modalStatusElement = document.getElementById("modalStatus");
    const modalImageElement = document.getElementById("modalImage");
    const modalImageFallbackElement = document.getElementById("modalImageFallback");

    const description = item.dataset.description || "";
    const created = item.dataset.created || "";
    const status = item.dataset.status || "";
    const imageUrl = item.dataset.image || "";

    if (modalDescriptionElement)
        modalDescriptionElement.textContent = description;

    if (modalCreatedElement)
        modalCreatedElement.textContent = created;

    if (modalStatusElement)
        modalStatusElement.textContent = status;

    if (modalImageElement && modalImageFallbackElement) {

        if (imageUrl.trim().length > 0) {

            modalImageElement.src = imageUrl;
            modalImageElement.classList.remove("d-none");

            modalImageFallbackElement.classList.add("d-none");
            modalImageFallbackElement.textContent = "";

        } else {

            modalImageElement.removeAttribute("src");
            modalImageElement.classList.add("d-none");

            modalImageFallbackElement.textContent =
                "No image was provided for this report.";

            modalImageFallbackElement.classList.remove("d-none");

        }
    }
  });

});


// basic HTML escaping so user text wont break DOM
function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}
