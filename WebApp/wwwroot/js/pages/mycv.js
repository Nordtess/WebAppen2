document.addEventListener("DOMContentLoaded", () => {
    const toggle = document.querySelector(".cv-toggle");
    if (toggle) {
        toggle.addEventListener("click", async () => {
            const isOn = toggle.classList.toggle("is-on");
            toggle.setAttribute("aria-pressed", isOn ? "true" : "false");

            try {
                const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
                const body = new URLSearchParams({ isPrivate: isOn ? "true" : "false" });

                await fetch("/MyCv/SetPrivacy", {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/x-www-form-urlencoded",
                        ...(token ? { "RequestVerificationToken": token } : {})
                    },
                    credentials: "same-origin",
                    body
                });
            } catch {
            }
        });
    }

    // --- Project picker modal (max 4) ---
    const modal = document.getElementById("mycv-projects-modal");
    const picker = document.getElementById("mycv-projects-picker");
    const counter = document.getElementById("mycv-projects-counter");

    function setModalOpen(open) {
        if (!modal) return;
        modal.setAttribute("aria-hidden", open ? "false" : "true");
        document.body.classList.toggle("mycv-modal-open", open);

        if (open) {
            // Focus the close button for accessibility.
            const closeBtn = modal.querySelector("[data-mycv-modal-close]");
            closeBtn?.focus();
        }
    }

    function updatePickerUi() {
        if (!picker) return;

        const checkboxes = Array.from(picker.querySelectorAll('input[type="checkbox"]'));
        const checked = checkboxes.filter(c => c.checked);
        const checkedCount = checked.length;

        if (counter) {
            counter.textContent = `${checkedCount}/4 valda`;
        }

        const limitReached = checkedCount >= 4;
        for (const cb of checkboxes) {
            if (cb.checked) {
                cb.disabled = false;
                cb.closest(".mycv-picker__row")?.classList.remove("is-disabled");
                continue;
            }

            cb.disabled = limitReached;
            cb.closest(".mycv-picker__row")?.classList.toggle("is-disabled", limitReached);
        }
    }

    document.addEventListener("click", (e) => {
        const t = e.target;
        if (!(t instanceof HTMLElement)) return;

        const openBtn = t.closest("[data-mycv-modal-open]");
        if (openBtn) {
            const id = openBtn.getAttribute("data-mycv-modal-open");
            if (id === "mycv-projects-modal") {
                setModalOpen(true);
                updatePickerUi();
            }
            if (id === "mycv-competences-modal") {
                const cm = document.getElementById("mycv-competences-modal");
                if (cm) {
                    cm.setAttribute("aria-hidden", "false");
                    document.body.classList.toggle("mycv-modal-open", true);
                }
            }
            return;
        }

        if (t.closest("[data-mycv-modal-close]")) {
            setModalOpen(false);
            const cm = document.getElementById("mycv-competences-modal");
            if (cm?.getAttribute("aria-hidden") === "false") {
                cm.setAttribute("aria-hidden", "true");
                document.body.classList.toggle("mycv-modal-open", false);
            }
        }
    });

    document.addEventListener("keydown", (e) => {
        if (e.key === "Escape" && modal?.getAttribute("aria-hidden") === "false") {
            setModalOpen(false);
        }
        const cm = document.getElementById("mycv-competences-modal");
        if (e.key === "Escape" && cm?.getAttribute("aria-hidden") === "false") {
            cm.setAttribute("aria-hidden", "true");
            document.body.classList.toggle("mycv-modal-open", false);
        }
    });

    picker?.addEventListener("change", (e) => {
        const t = e.target;
        if (!(t instanceof HTMLInputElement)) return;
        if (t.type !== "checkbox") return;

        updatePickerUi();
    });

    updatePickerUi();
});
