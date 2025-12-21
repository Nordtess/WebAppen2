(() => {
    const root = document.querySelector('[data-carousel="cv"]');
    if (!root) return;

    const slides = Array.from(root.querySelectorAll(".cv-slide"));
    const dots = Array.from(root.querySelectorAll(".cv-dot"));
    const btnPrev = root.querySelector(".cv-nav--prev");
    const btnNext = root.querySelector(".cv-nav--next");

    let index = slides.findIndex((s) => s.classList.contains("is-active"));
    if (index < 0) index = 0;

    const clampIndex = (i) => (i + slides.length) % slides.length;

    function render() {
        // Sätt endast tre visuella lägen: vänster, aktiv (mitten) och höger.
        const leftIndex = clampIndex(index - 1);
        const rightIndex = clampIndex(index + 1);

        slides.forEach((slide, i) => {
            slide.classList.remove("is-left", "is-right", "is-active");

            if (i === index) slide.classList.add("is-active");
            else if (i === leftIndex) slide.classList.add("is-left");
            else if (i === rightIndex) slide.classList.add("is-right");
        });

        dots.forEach((dot, i) => {
            dot.classList.toggle("is-active", i === index);
        });
    }

    function goTo(i) {
        index = clampIndex(i);
        render();
    }

    btnPrev?.addEventListener("click", () => goTo(index - 1));
    btnNext?.addEventListener("click", () => goTo(index + 1));

    dots.forEach((dot, i) => dot.addEventListener("click", () => goTo(i)));

    slides.forEach((slide) => {
        slide.addEventListener("click", (e) => {
            // Klick på sidokort ska inte navigera.
            if (!slide.classList.contains("is-active")) {
                e.preventDefault();
                return;
            }

            // Om kortet saknar riktig länk (t.ex. data-href eller href="#"), förhindra hopp.
            const href = slide.getAttribute("data-href") || slide.getAttribute("href");
            if (!href || href === "#") {
                e.preventDefault();
            }
        });
    });

    render();
})();
