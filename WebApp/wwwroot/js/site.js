document.addEventListener("click", (e) => {
    const btn = document.querySelector(".nl-burger");
    const panel = document.querySelector(".nl-menu-panel");
    if (!btn || !panel) return;

    const clickedBtn = btn.contains(e.target);
    const clickedPanel = panel.contains(e.target);

    if (clickedBtn) {
        const isOpen = panel.classList.toggle("open");
        btn.classList.toggle("is-open", isOpen);

        btn.setAttribute("aria-expanded", isOpen ? "true" : "false");
        panel.setAttribute("aria-hidden", isOpen ? "false" : "true");
        return;
    }

    if (!clickedPanel) {
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
});
