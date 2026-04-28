// Handles upvote toggling on the Details page and the Latest Reports modal.
// POST /Vote/Toggle/{id} returns { voteCount, userHasVoted }

(function () {

    // ── Details page vote button ─────────────────────────────────────────────

    const voteBtn = document.getElementById("voteBtn");
    const voteCount = document.getElementById("voteCount");

    if (voteBtn && voteCount) {
        voteBtn.addEventListener("click", async function () {
            const reportId = voteBtn.dataset.reportId;

            try {
                const response = await fetch(`/Vote/Toggle/${reportId}`, {
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
                voteCount.textContent = data.voteCount;
                updateVoteButton(voteBtn, data.userHasVoted);

                // Update the plural text node without replacing the #voteCount span
                const label = voteBtn.closest(".d-flex")?.querySelector(".text-muted");
                if (label) {
                    const textNode = Array.from(label.childNodes)
                        .find(n => n.nodeType === Node.TEXT_NODE);
                    if (textNode) {
                        textNode.textContent = ` community vote${data.voteCount === 1 ? "" : "s"}`;
                    }
                }

            } catch (err) {
                console.error("Vote request failed:", err);
            }
        });
    }

    // ── Latest Reports modal vote button ────────────────────────────────────

    const modalVoteBtn = document.getElementById("modalVoteBtn");
    const modalVoteCount = document.getElementById("modalVoteCount");
    const modalVotePlural = document.getElementById("modalVotePlural");

    if (modalVoteBtn && modalVoteCount) {

        // When the modal opens, load vote status for the selected report
        const reportModal = document.getElementById("reportModal");
        if (reportModal) {
            reportModal.addEventListener("show.bs.modal", async function (event) {
                const trigger = event.relatedTarget;
                const reportId = trigger?.dataset?.reportid;

                if (!reportId) return;

                modalVoteBtn.dataset.reportId = reportId;

                // Fetch current vote status
                try {
                    const response = await fetch(`/Vote/Status/${reportId}`);
                    if (!response.ok) return;

                    const data = await response.json();
                    modalVoteCount.textContent = data.voteCount;
                    modalVotePlural.textContent = data.voteCount === 1 ? "" : "s";
                    updateVoteButton(modalVoteBtn, data.userHasVoted);
                } catch (err) {
                    console.error("Failed to load vote status:", err);
                }
            });
        }

        modalVoteBtn.addEventListener("click", async function () {
            const reportId = modalVoteBtn.dataset.reportId;
            if (!reportId) return;

            try {
                const response = await fetch(`/Vote/Toggle/${reportId}`, {
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
                modalVoteCount.textContent = data.voteCount;
                modalVotePlural.textContent = data.voteCount === 1 ? "" : "s";
                updateVoteButton(modalVoteBtn, data.userHasVoted);

            } catch (err) {
                console.error("Vote request failed:", err);
            }
        });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    function updateVoteButton(btn, hasVoted) {
        if (hasVoted) {
            btn.classList.remove("btn-outline-primary");
            btn.classList.add("btn-primary");
        } else {
            btn.classList.remove("btn-primary");
            btn.classList.add("btn-outline-primary");
        }
    }

    function getAntiForgeryToken() {
        const field = document.querySelector('input[name="__RequestVerificationToken"]');
        return field ? field.value : "";
    }

})();
