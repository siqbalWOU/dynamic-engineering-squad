// Handles fix verification toggling on the Details page.
// POST /VerifyFix/Toggle/{id} returns { verifyCount, userHasVerified, threshold, verified }

(function () {

    const verifyBtn   = document.getElementById("verifyBtn");
    const verifyCount = document.getElementById("verifyCount");

    if (!verifyBtn || !verifyCount) return;

    verifyBtn.addEventListener("click", async function () {
        const reportId = verifyBtn.dataset.reportId;

        try {
            const response = await fetch(`/VerifyFix/Toggle/${reportId}`, {
                method: "POST",
                headers: {
                    "RequestVerificationToken": getAntiForgeryToken()
                }
            });

            if (!response.ok) {
                if (response.status === 401) {
                    window.location.href = "/Account/Login";
                }
                return;
            }

            const data = await response.json();

            if (data.verified) {
                // Threshold reached — replace button and count with verified badge
                const container = verifyBtn.closest(".d-flex");
                if (container) {
                    container.innerHTML = `
                        <span class="badge bg-success fs-6 px-3 py-2">
                            <i class="fa-solid fa-circle-check me-1"></i> Verified Fixed
                        </span>`;
                }
            } else {
                verifyCount.textContent = data.verifyCount;
                verifyBtn.classList.toggle("btn-outline-success", !data.userHasVerified);
                verifyBtn.classList.toggle("btn-success", data.userHasVerified);
            }

        } catch (err) {
            console.error("Verify request failed:", err);
        }
    });

    function getAntiForgeryToken() {
        const field = document.querySelector('input[name="__RequestVerificationToken"]');
        return field ? field.value : "";
    }

})();
