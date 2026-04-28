// Feature-81: Latest Reports modal population
document.addEventListener("DOMContentLoaded", function () {

    const reportModal = document.getElementById("reportModal");
    if (!reportModal) {
        return;
    }

    const modalDescriptionElement = document.getElementById("modalDescription");
    const modalCreatedElement = document.getElementById("modalCreated");
    const modalStatusElement = document.getElementById("modalStatus");

    const modalVerifySection = document.getElementById("modalVerifySection");
    const modalVerifyBtn = document.getElementById("modalVerifyBtn");
    const modalVerifyCount = document.getElementById("modalVerifyCount");
    const modalVerifyPlural = document.getElementById("modalVerifyPlural");

    const modalFlagBtn = document.getElementById("modalFlagBtn");
    const modalImageElement = document.getElementById("modalImage");
    const modalImageFallbackElement = document.getElementById("modalImageFallback");
    const openFullReportLink = document.getElementById("openFullReportLink");

    const latestReportsUrl = "/Reports/Latest";

    function getAntiForgeryToken() {
        const field = document.querySelector('input[name="__RequestVerificationToken"]');
        return field ? field.value : "";
    }

    async function loadVerifyStatus(reportId) {
        if (!modalVerifySection) return;
        try {
            const res = await fetch(`/VerifyFix/Status/${reportId}`);
            if (!res.ok) return;
            const data = await res.json();
            updateVerifyUI(data);
        } catch { }
    }

    async function loadFlagStatus(reportId) {
        if (!modalFlagBtn) return;
        try {
            const res = await fetch(`/Flag/Status?reportId=${reportId}`);
            if (!res.ok) return;
            const data = await res.json();
            updateFlagUI(data.hasUserFlagged);
        } catch { }
    }

    function updateFlagUI(hasUserFlagged) {
        if (!modalFlagBtn) return;

        const iconMarkup = '<i class="fa-solid fa-flag me-1"></i>';
        modalFlagBtn.disabled = hasUserFlagged;
        modalFlagBtn.innerHTML = hasUserFlagged ? `${iconMarkup}Already Flagged` : `${iconMarkup}Flag`;

        if (hasUserFlagged) {
            modalFlagBtn.classList.remove("btn-outline-danger");
            modalFlagBtn.classList.add("btn-secondary");
        } else {
            modalFlagBtn.classList.remove("btn-secondary");
            modalFlagBtn.classList.add("btn-outline-danger");
        }
    }

    function updateVerifyUI(data) {
        if (!modalVerifySection || !modalVerifyCount) return;

        modalVerifyCount.textContent = data.verifyCount;
        if (modalVerifyPlural) {
            modalVerifyPlural.textContent = data.verifyCount === 1 ? "" : "s";
        }

        if (modalVerifyBtn) {
            modalVerifyBtn.classList.toggle("btn-outline-success", !data.userHasVerified);
            modalVerifyBtn.classList.toggle("btn-success", data.userHasVerified);
        }
    }

    function pushReportUrl(reportId) {
        if (!reportId) return;

        const newUrl = `/ReportIssue/Details/${reportId}`;
        if (window.location.pathname !== newUrl) {
            history.pushState({ reportId: reportId }, "", newUrl);
        }
    }

    function restoreLatestReportsUrl() {
        if (window.location.pathname !== latestReportsUrl) {
            history.replaceState({}, "", latestReportsUrl);
        }
    }

    function showModal(modalElement) {
        if (window.bootstrap?.Modal) {
            bootstrap.Modal.getOrCreateInstance(modalElement).show();
            return;
        }

        modalElement.style.display = "block";
        modalElement.removeAttribute("aria-hidden");
        modalElement.setAttribute("aria-modal", "true");
        modalElement.classList.add("show");
        document.body.classList.add("modal-open");
    }

    function populateModalImage(imageUrl) {
        if (!modalImageElement || !modalImageFallbackElement) {
            return;
        }

        modalImageElement.onerror = null;

        if (imageUrl.trim().length > 0) {
            modalImageElement.onerror = function () {
                showBrokenImageFallback(modalImageElement, modalImageFallbackElement);
            };

            modalImageElement.src = imageUrl;
            modalImageElement.classList.remove("d-none");
            modalImageFallbackElement.classList.add("d-none");
            modalImageFallbackElement.innerHTML = "";
            return;
        }

        showMissingImageFallback(modalImageElement, modalImageFallbackElement);
    }

    function populateReportModal(item) {
        const description = item.dataset.description || "";
        const created = item.dataset.created || "";
        const status = item.dataset.status || "";
        const imageUrl = item.dataset.image || "";
        const reportId = item.dataset.reportid || item.dataset.reportId || "";

        if (openFullReportLink && reportId) {
            openFullReportLink.href = `/ReportIssue/Details/${reportId}`;
        }

        if (modalVerifySection && modalVerifyBtn) {
            if (status === "Approved") {
                modalVerifyBtn.dataset.reportId = reportId;
                modalVerifySection.classList.remove("d-none");
                if (modalVerifyCount) {
                    modalVerifyCount.textContent = "0";
                }
                loadVerifyStatus(reportId);
            } else {
                modalVerifySection.classList.add("d-none");
            }
        }

        if (modalFlagBtn) {
            modalFlagBtn.dataset.reportId = reportId;
            loadFlagStatus(reportId);
        }

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

        populateModalImage(imageUrl);
    }

    window.openLatestReportModal = function (item) {
        if (!item) {
            return;
        }

        populateReportModal(item);
        showModal(reportModal);
    };

    reportModal.addEventListener("hidden.bs.modal", function () {
        restoreLatestReportsUrl();
    });

    window.addEventListener("popstate", function () {
        const modalInstance = bootstrap.Modal.getInstance(reportModal);
        if (modalInstance) {
            modalInstance.hide();
        }
    });

    document.addEventListener("click", function (event) {
        const item = event.target.closest(".report-item");
        if (!item) {
            return;
        }

        window.openLatestReportModal(item);
    });

    if (modalVerifyBtn) {
        modalVerifyBtn.addEventListener("click", async function () {
            const reportId = modalVerifyBtn.dataset.reportId;

            try {
                const res = await fetch(`/VerifyFix/Toggle/${reportId}`, {
                    method: "POST",
                    headers: { "RequestVerificationToken": getAntiForgeryToken() }
                });

                if (!res.ok) {
                    if (res.status === 401) {
                        window.location.href = "/Account/Login";
                    }
                    return;
                }

                const data = await res.json();
                updateVerifyUI(data);
            } catch (err) {
                console.error("Verify request failed:", err);
            }
        });
    }
});

// SCRUM-101 test helper
function setReportDetailsLink(linkElement, reportId) {
    if (linkElement && reportId) {
        linkElement.href = `/ReportIssue/Details/${reportId}`;
    }
}

// SCRUM-101 test helper
const latestReportsUrl = "/Reports/Latest";

// SCRUM-101 test helper
function pushReportUrl(reportId) {
    if (!reportId) return;

    const newUrl = `/ReportIssue/Details/${reportId}`;
    if (window.location.pathname !== newUrl) {
        history.pushState({ reportId: reportId }, "", newUrl);
    }
}

// SCRUM-101 test helper
function restoreLatestReportsUrl() {
    if (window.location.pathname !== latestReportsUrl) {
        history.replaceState({}, "", latestReportsUrl);
    }
}

// SCRUM-102 test helpers
function showMissingImageFallback(modalImageElement, modalImageFallbackElement) {
    modalImageElement.removeAttribute("src");
    modalImageElement.classList.add("d-none");

    modalImageFallbackElement.innerHTML =
        "<strong>ðŸ“· No image available.</strong> This report was submitted without an image.";

    modalImageFallbackElement.classList.remove("d-none");
}

function showBrokenImageFallback(modalImageElement, modalImageFallbackElement) {
    modalImageElement.removeAttribute("src");
    modalImageElement.classList.add("d-none");

    modalImageFallbackElement.innerHTML =
        "<strong>âš  Image could not be loaded.</strong> The image file may be missing.";

    modalImageFallbackElement.classList.remove("d-none");
}

if (typeof module !== "undefined") {
    module.exports = {
        setReportDetailsLink,
        pushReportUrl,
        restoreLatestReportsUrl,
        showMissingImageFallback,
        showBrokenImageFallback
    };
}
