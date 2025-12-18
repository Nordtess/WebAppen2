document.addEventListener("click", (e) => {
    const btn = document.querySelector(".nl-burger");
    const panel = document.querySelector(".nl-menu-panel");
    if (!btn || !panel) return;

    const clickedBtn = btn.contains(e.target);
    const clickedPanel = panel.contains(e.target);

    if (clickedBtn) {
        e.preventDefault();
        e.stopPropagation();

        const isOpen = panel.classList.toggle("open");
        btn.classList.toggle("is-open", isOpen);

        btn.setAttribute("aria-expanded", isOpen ? "true" : "false");
        panel.setAttribute("aria-hidden", isOpen ? "false" : "true");
        return;
    }

    if (!clickedPanel && panel.classList.contains("open")) {
        panel.classList.remove("open");
        btn.classList.remove("is-open");
        btn.setAttribute("aria-expanded", "false");
        panel.setAttribute("aria-hidden", "true");
    }
});

document.addEventListener("keydown", (e) => {
    if (e.key !== "Escape") return;

    const btn = document.querySelector(".nl-burger");
    const panel = document.querySelector(".nl-menu-panel");
    if (!btn || !panel) return;

    panel.classList.remove("open");
    btn.classList.remove("is-open");
    btn.setAttribute("aria-expanded", "false");
    panel.setAttribute("aria-hidden", "true");
    btn.focus();
});

document.addEventListener("DOMContentLoaded", () => {
    // Stäng meny när man klickar item
    document.querySelectorAll(".nl-menu-item").forEach((item) => {
        item.addEventListener("click", () => {
            const btn = document.querySelector(".nl-burger");
            const panel = document.querySelector(".nl-menu-panel");
            if (!btn || !panel) return;

            panel.classList.remove("open");
            btn.classList.remove("is-open");
            btn.setAttribute("aria-expanded", "false");
            panel.setAttribute("aria-hidden", "true");
        });
    });

    // Mail icon: closed/open baserat på unread
    const mail = document.querySelector(".nl-mail");
    if (mail) {
        const unread = parseInt(mail.dataset.unread || "0", 10);
        const closedSrc = mail.dataset.closedSrc;
        const openSrc = mail.dataset.openSrc;

        const img = mail.querySelector(".nl-mail-img");
        const countEl = mail.querySelector(".nl-unread-count");

        if (countEl) countEl.textContent = String(Math.max(0, unread));

        if (img && closedSrc && openSrc) {
            img.src = unread > 0 ? openSrc : closedSrc;
        }

        // valfritt: ändra text vid 0
        if (unread <= 0) {
            const txt = mail.querySelector(".nl-mail-text");
            if (txt) txt.innerHTML = "Inga nya meddelanden";
        }
    }

    // User menu dropdown toggle
    document.addEventListener("click", (e) => {
        const toggle = document.querySelector(".user-menu-toggle");
        const dropdown = document.querySelector(".user-dropdown");
        
        if (!toggle || !dropdown) return;

        const clickedToggle = toggle.contains(e.target);
        const clickedDropdown = dropdown.contains(e.target);

        if (clickedToggle) {
            e.preventDefault();
            e.stopPropagation();

            const isOpen = dropdown.getAttribute("aria-hidden") === "false";
            
            dropdown.setAttribute("aria-hidden", isOpen ? "true" : "false");
            toggle.setAttribute("aria-expanded", isOpen ? "false" : "true");
            
            return;
        }

        if (!clickedDropdown && dropdown.getAttribute("aria-hidden") === "false") {
            dropdown.setAttribute("aria-hidden", "true");
            toggle.setAttribute("aria-expanded", "false");
        }
    });

    // Close dropdown on Escape key
    document.addEventListener("keydown", (e) => {
        if (e.key !== "Escape") return;

        const toggle = document.querySelector(".user-menu-toggle");
        const dropdown = document.querySelector(".user-dropdown");
        
        if (!toggle || !dropdown) return;

        if (dropdown.getAttribute("aria-hidden") === "false") {
            dropdown.setAttribute("aria-hidden", "true");
            toggle.setAttribute("aria-expanded", "false");
            toggle.focus();
        }
    });

    // Close dropdown when clicking dropdown items
    document.querySelectorAll(".dropdown-item").forEach((item) => {
        item.addEventListener("click", () => {
            const toggle = document.querySelector(".user-menu-toggle");
            const dropdown = document.querySelector(".user-dropdown");
            
            if (!toggle || !dropdown) return;

            dropdown.setAttribute("aria-hidden", "true");
            toggle.setAttribute("aria-expanded", "false");
        });
    });
});

// Close dropdown on window resize
let resizeTimer;
window.addEventListener("resize", () => {
    clearTimeout(resizeTimer);
    resizeTimer = setTimeout(() => {
        const toggle = document.querySelector(".user-menu-toggle");
        const dropdown = document.querySelector(".user-dropdown");
        
        if (!toggle || !dropdown) return;

        if (dropdown.getAttribute("aria-hidden") === "false") {
            dropdown.setAttribute("aria-hidden", "true");
            toggle.setAttribute("aria-expanded", "false");
        }
    }, 250);
});
