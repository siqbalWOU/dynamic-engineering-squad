document.addEventListener('DOMContentLoaded', () => {
    const flagForm = document.getElementById('flagForm');
    const submitFlagBtn = document.getElementById('submitFlagBtn');
    const flagMessage = document.getElementById('flagMessage');
    const flagModalEl = document.getElementById('flagModal');
    const flagReportIdInput = document.getElementById('flagReportId');
    function showFlagModal() {
        if (!flagModalEl) {
            return;
        }

        if (window.bootstrap?.Modal) {
            bootstrap.Modal.getOrCreateInstance(flagModalEl).show();
        } else {
            flagModalEl.style.display = 'block';
            flagModalEl.removeAttribute('aria-hidden');
            flagModalEl.setAttribute('aria-modal', 'true');
            flagModalEl.classList.add('show');
            document.body.classList.add('modal-open');
        }
    }

    function hideFlagModal() {
        if (!flagModalEl) {
            return;
        }

        if (window.bootstrap?.Modal) {
            const instance = bootstrap.Modal.getInstance(flagModalEl);
            if (instance) {
                instance.hide();
            }
            // Cleanup in case of stacking issues
            setTimeout(() => {
                if (!document.querySelector('.modal.show')) {
                    document.body.classList.remove('modal-open');
                    const backdrop = document.querySelector('.modal-backdrop');
                    if (backdrop) backdrop.remove();
                }
            }, 500);
        } else {
            flagModalEl.classList.remove('show');
            flagModalEl.setAttribute('aria-hidden', 'true');
            flagModalEl.removeAttribute('aria-modal');
            flagModalEl.style.display = 'none';
            document.body.classList.remove('modal-open');
        }

        if (flagForm) {
            flagForm.reset();
        }
        if (flagMessage) {
            flagMessage.classList.add('d-none');
            flagMessage.textContent = '';
        }
        if (submitFlagBtn) {
            submitFlagBtn.disabled = false;
            submitFlagBtn.textContent = 'Submit Report';
        }
    }

    window.showFlagModalFromButton = function (btn) {
        if (!btn || btn.disabled) {
            return;
        }

        const reportId = btn.getAttribute('data-report-id');
        if (flagReportIdInput) {
            flagReportIdInput.value = reportId;
        }

        showFlagModal();
    };

    // Use event delegation or check for elements
    document.addEventListener('click', (e) => {
        const btn = e.target.closest('#flagBtn, #modalFlagBtn');
        if (btn) {
            window.showFlagModalFromButton(btn);
        }
    });

    if (submitFlagBtn && flagForm) {
        submitFlagBtn.addEventListener('click', async () => {
            const reportId = flagReportIdInput.value;
            const categoryElement = flagForm.querySelector('input[name="category"]:checked');
            
            if (!reportId || !categoryElement) return;
            
            const category = categoryElement.value;

            submitFlagBtn.disabled = true;
            submitFlagBtn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Submitting...';

            try {
                const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
                const token = tokenElement ? tokenElement.value : '';

                const response = await fetch('/Flag/Create', {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': token,
                        'Content-Type': 'application/x-www-form-urlencoded'
                    },
                    body: new URLSearchParams({
                        reportId: reportId,
                        category: category
                    })
                });

                const result = await response.json();

                if (result.success) {
                    flagMessage.textContent = result.message;
                    flagMessage.classList.remove('d-none', 'alert-danger');
                    flagMessage.classList.add('alert-success');

                    // Update UI buttons
                    const buttonsToUpdate = document.querySelectorAll(`button[data-report-id="${reportId}"][id*="flagBtn"], button[id="modalFlagBtn"], button[id="flagBtn"]`);
                    buttonsToUpdate.forEach(btn => {
                        if (btn.getAttribute('data-report-id') === reportId || (btn.id === 'flagBtn' && !btn.getAttribute('data-report-id'))) {
                            btn.disabled = true;
                            btn.textContent = 'Already Flagged';
                            btn.classList.remove('btn-outline-danger');
                            btn.classList.add('btn-secondary');
                        }
                    });

                    setTimeout(() => {
                        hideFlagModal();
                    }, 1500);
                } else {
                    flagMessage.textContent = result.message || 'An error occurred.';
                    flagMessage.classList.remove('d-none', 'alert-success');
                    flagMessage.classList.add('alert-danger');
                    submitFlagBtn.disabled = false;
                    submitFlagBtn.textContent = 'Submit Report';
                }
            } catch (error) {
                console.error('Error flagging report:', error);
                flagMessage.textContent = 'A network error occurred. Please try again.';
                flagMessage.classList.remove('d-none', 'alert-success');
                flagMessage.classList.add('alert-danger');
                submitFlagBtn.disabled = false;
                submitFlagBtn.textContent = 'Submit Report';
            }
        });
    }
});
