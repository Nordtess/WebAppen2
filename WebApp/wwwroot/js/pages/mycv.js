document.addEventListener("DOMContentLoaded", () => {
    const toggle = document.querySelector(".cv-toggle");
    if (!toggle) return;

    toggle.addEventListener("click", () => {
        const isOn = toggle.classList.toggle("is-on");
        toggle.setAttribute("aria-checked", isOn ? "true" : "false");
    });
});
