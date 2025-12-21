document.addEventListener("DOMContentLoaded", () => {
    const toggle = document.querySelector(".cv-toggle");
    if (!toggle) return;

    toggle.addEventListener("click", () => {
        // Växlar UI-tillstånd (klass + aria) för integritetsknappen.
        const isOn = toggle.classList.toggle("is-on");
        toggle.setAttribute("aria-pressed", isOn ? "true" : "false");
    });
});
