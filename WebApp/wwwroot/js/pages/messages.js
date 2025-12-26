document.addEventListener("DOMContentLoaded", () => {
    const unreadPill = document.getElementById("messagesUnreadPill");

    function getUnreadCount() {
        if (!unreadPill) return 0;
        const m = (unreadPill.textContent || "").match(/(\d+)/);
        return m ? parseInt(m[1], 10) : 0;
    }

    function setUnreadCount(n) {
        if (!unreadPill) return;
        const v = Math.max(0, Number.isFinite(n) ? n : 0);
        unreadPill.textContent = `${v} olästa`;
        unreadPill.classList.toggle("messages-unread-pill--ok", v <= 0);

        // keep header icon/text in sync if present
        const headerUnreadText = document.querySelector(".unread-text");
        const headerUnreadCount = document.getElementById("headerUnreadCount");
        const headerNocco = document.getElementById("headerNocco");

        if (headerUnreadCount) headerUnreadCount.textContent = String(v);

        if (headerUnreadText instanceof HTMLElement) {
            headerUnreadText.style.display = v > 0 ? "inline" : "none";
        }

        if (headerNocco instanceof HTMLImageElement) {
            const sleep = headerNocco.getAttribute("data-sleep-src");
            const msg = headerNocco.getAttribute("data-message-src");
            const next = v > 0 ? msg : sleep;
            if (next) headerNocco.src = next;
        }
    }

    function decUnread() {
        setUnreadCount(getUnreadCount() - 1);
    }

    function incUnread() {
        setUnreadCount(getUnreadCount() + 1);
    }

    function updateCardReadState(card, isRead) {
        card.dataset.isread = isRead ? "1" : "0";

        const dot = card.querySelector(".message-dot");
        if (dot) dot.remove();

        if (!isRead) {
            const from = card.querySelector(".message-from");
            if (from) {
                const d = document.createElement("span");
                d.className = "message-dot";
                d.title = "Oläst";
                d.setAttribute("aria-label", "Oläst");
                from.appendChild(d);
            }
        }

        const form = card.querySelector("[data-setread-form]");
        if (form) {
            const val = form.querySelector("[data-setread-value]");
            const btn = form.querySelector("[data-setread-btn]");
            if (val instanceof HTMLInputElement) val.value = isRead ? "true" : "false";
            if (btn instanceof HTMLButtonElement) btn.textContent = isRead ? "Markera som oläst" : "Markera som läst";
        }
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

    // Expand/collapse + auto-mark as read on open
    document.querySelectorAll("[data-message]").forEach((card) => {
        const toggle = card.querySelector("[data-message-toggle]");
        const body = card.querySelector(".message-body");
        if (!(toggle instanceof HTMLButtonElement) || !(body instanceof HTMLElement)) return;

        toggle.addEventListener("click", async () => {
            const open = toggle.getAttribute("aria-expanded") === "true";
            const nextOpen = !open;

            toggle.setAttribute("aria-expanded", nextOpen ? "true" : "false");
            body.hidden = !nextOpen;
            card.classList.toggle("is-open", nextOpen);

            // Auto-mark as read when opening.
            const isRead = card.getAttribute("data-isread") === "1";
            if (nextOpen && !isRead) {
                const form = card.querySelector("[data-setread-form]");
                if (form instanceof HTMLFormElement) {
                    // Optimistic UI
                    updateCardReadState(card, true);
                    decUnread();

                    const val = form.querySelector("[data-setread-value]");
                    if (val instanceof HTMLInputElement) val.value = "true";

                    try {
                        const res = await postForm(form);
                        if (!res.ok) {
                            updateCardReadState(card, false);
                            incUnread();
                        }
                    } catch {
                        updateCardReadState(card, false);
                        incUnread();
                    }
                }
            }
        });

        // Manual read/unread toggle uses AJAX.
        const setReadForm = card.querySelector("[data-setread-form]");
        if (setReadForm instanceof HTMLFormElement) {
            setReadForm.addEventListener("submit", async (e) => {
                e.preventDefault();

                const currentIsRead = card.getAttribute("data-isread") === "1";
                const nextIsRead = !currentIsRead;

                // Optimistic UI
                updateCardReadState(card, nextIsRead);
                if (currentIsRead && !nextIsRead) incUnread();
                else if (!currentIsRead && nextIsRead) decUnread();

                const val = setReadForm.querySelector("[data-setread-value]");
                if (val instanceof HTMLInputElement) val.value = nextIsRead ? "true" : "false";

                try {
                    const res = await postForm(setReadForm);
                    if (!res.ok) {
                        // rollback
                        updateCardReadState(card, currentIsRead);
                        if (currentIsRead && !nextIsRead) decUnread();
                        else if (!currentIsRead && nextIsRead) incUnread();
                    }
                } catch {
                    updateCardReadState(card, currentIsRead);
                    if (currentIsRead && !nextIsRead) decUnread();
                    else if (!currentIsRead && nextIsRead) incUnread();
                }
            });
        }
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
                text.textContent = from ? `Vill du ta bort meddelandet från ${from}?` : "Vill du ta bort meddelandet?";
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

    // AJAX delete: remove card + update unread pill instantly.
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
});
