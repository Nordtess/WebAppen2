document.addEventListener("DOMContentLoaded", () => {
    /** @type {HTMLElement | null} */
    const headerUnreadText = document.querySelector(".unread-text");
    /** @type {HTMLElement | null} */
    const headerUnreadCount = document.getElementById("headerUnreadCount");
    /** @type {HTMLImageElement | null} */
    const headerNocco = document.getElementById("headerNocco");

    function getHeaderUnreadCount() {
        const n = Number(headerUnreadCount?.textContent || "0");
        return Number.isFinite(n) ? n : 0;
    }

    function setHeaderUnreadCount(n) {
        const v = Math.max(0, Number.isFinite(n) ? n : 0);

        if (headerUnreadCount) headerUnreadCount.textContent = String(v);

        if (headerUnreadText) {
            headerUnreadText.style.display = v > 0 ? "inline" : "none";
        }

        if (headerNocco) {
            const sleep = headerNocco.getAttribute("data-sleep-src");
            const msg = headerNocco.getAttribute("data-message-src");
            const next = v > 0 ? msg : sleep;
            if (next) headerNocco.src = next;
        }
    }

    function decUnread() {
        setHeaderUnreadCount(getHeaderUnreadCount() - 1);
    }

    function incUnread() {
        setHeaderUnreadCount(getHeaderUnreadCount() + 1);
    }

    async function postForm(form) {
        const action = form.getAttribute("action") || "";
        const method = (form.getAttribute("method") || "POST").toUpperCase();
        const fd = new FormData(form);

        return await fetch(action, {
            method,
            body: fd,
            credentials: "same-origin"
        });
    }

    // Expand/collapse only
    document.querySelectorAll("[data-message]").forEach((card) => {
        const toggle = card.querySelector("[data-message-toggle]");
        const body = card.querySelector(".message-body");
        if (!(toggle instanceof HTMLButtonElement) || !(body instanceof HTMLElement)) return;

        toggle.addEventListener("click", () => {
            const open = toggle.getAttribute("aria-expanded") === "true";
            const nextOpen = !open;

            toggle.setAttribute("aria-expanded", nextOpen ? "true" : "false");
            body.hidden = !nextOpen;
            card.classList.toggle("is-open", nextOpen);
        });
    });

    // Delete modal
    const modal = document.getElementById("deleteModal");
    const idInput = document.getElementById("deleteIdInput");
    const text = document.getElementById("deleteModalText");
    const deleteForm = document.querySelector("[data-delete-form]");

    function setOpen(open) {
        if (!modal) return;
        modal.setAttribute("aria-hidden", open ? "false" : "true");
        document.body.classList.toggle("messages-modal-open", open);

        if (open) {
            const btn = modal.querySelector(".messages-modal__close");
            if (btn instanceof HTMLButtonElement) btn.focus();
        }
    }

    let pendingDeleteId = null;

    document.addEventListener("click", (e) => {
        const t = e.target;
        if (!(t instanceof HTMLElement)) return;

        const del = t.closest("[data-delete-open]");
        if (del) {
            const id = del.getAttribute("data-delete-id") || "";
            const from = del.getAttribute("data-delete-from") || "";

            pendingDeleteId = id;

            if (idInput instanceof HTMLInputElement) idInput.value = id;
            if (text) {
                text.textContent = from
                    ? `Vill du ta bort meddelandet skickat av ${from}?`
                    : "Vill du ta bort meddelandet?";
            }

            setOpen(true);
            return;
        }

        if (t.closest("[data-delete-cancel]")) {
            setOpen(false);
            pendingDeleteId = null;
            return;
        }
    });

    document.addEventListener("keydown", (e) => {
        if (e.key === "Escape" && modal?.getAttribute("aria-hidden") === "false") {
            setOpen(false);
            pendingDeleteId = null;
        }
    });

    // AJAX delete: remove card + update header unread instantly.
    if (deleteForm instanceof HTMLFormElement) {
        deleteForm.addEventListener("submit", async (e) => {
            e.preventDefault();

            const id = pendingDeleteId;
            if (!id) return;

            const card = document.querySelector(`[data-message][data-id="${CSS.escape(id)}"]`);
            const wasUnread = card?.getAttribute("data-isread") === "0";

            // Optimistic UI
            if (card) card.remove();
            if (wasUnread) decUnread();

            setOpen(false);
            pendingDeleteId = null;

            try {
                const res = await postForm(deleteForm);
                if (!res.ok) window.location.reload();
            } catch {
                window.location.reload();
            }
        });
    }

    // Wire up read checkbox toggle (AJAX)
    document.querySelectorAll("[data-setread-form]").forEach((formEl) => {
        if (!(formEl instanceof HTMLFormElement)) return;

        const card = formEl.closest("[data-message]");
        if (!(card instanceof HTMLElement)) return;

        const chk = formEl.querySelector("[data-setread-check]");
        const val = formEl.querySelector("[data-setread-value]");
        if (!(chk instanceof HTMLInputElement) || chk.type !== "checkbox") return;
        if (!(val instanceof HTMLInputElement)) return;

        chk.addEventListener("change", async () => {
            const currentIsRead = card.getAttribute("data-isread") === "1";
            const nextIsRead = chk.checked;

            // Optimistic UI
            card.dataset.isread = nextIsRead ? "1" : "0";
            val.value = nextIsRead ? "true" : "false";

            const dot = card.querySelector(".message-dot");
            if (nextIsRead) {
                if (dot) dot.remove();
                if (!currentIsRead) decUnread();
            } else {
                if (!dot) {
                    const from = card.querySelector(".message-from");
                    if (from) {
                        const d = document.createElement("span");
                        d.className = "message-dot";
                        from.appendChild(d);
                    }
                }
                if (currentIsRead) incUnread();
            }

            try {
                const res = await postForm(formEl);
                if (!res.ok) window.location.reload();
            } catch {
                window.location.reload();
            }
        });
    });
});
