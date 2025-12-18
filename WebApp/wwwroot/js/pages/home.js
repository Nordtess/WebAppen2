(() => {
    const root = document.querySelector('[data-carousel="cv"]');
    if (!root) return;

    const slides = Array.from(root.querySelectorAll('.cv-slide'));
    const dots = Array.from(root.querySelectorAll('.cv-dot'));
    const btnPrev = root.querySelector('.cv-nav--prev');
    const btnNext = root.querySelector('.cv-nav--next');

    let index = slides.findIndex(s => s.classList.contains('is-active'));
    if (index < 0) index = 0;

    const clampIndex = (i) => (i + slides.length) % slides.length;

    function render() {
        slides.forEach((s, i) => {
            s.classList.remove('is-left', 'is-right', 'is-active');

            // default: hidden (CSS handles opacity/position)
            const leftIndex = clampIndex(index - 1);
            const rightIndex = clampIndex(index + 1);

            if (i === index) s.classList.add('is-active');
            else if (i === leftIndex) s.classList.add('is-left');
            else if (i === rightIndex) s.classList.add('is-right');
        });

        dots.forEach((d, i) => {
            d.classList.toggle('is-active', i === index);
        });
    }

    function goTo(i) {
        index = clampIndex(i);
        render();
    }

    btnPrev?.addEventListener('click', () => goTo(index - 1));
    btnNext?.addEventListener('click', () => goTo(index + 1));

    dots.forEach((d, i) => d.addEventListener('click', () => goTo(i)));

    // Click on active slide navigates (placeholder)
    slides.forEach((slide) => {
        slide.addEventListener('click', (e) => {
            if (!slide.classList.contains('is-active')) {
                e.preventDefault();
                return;
            }

            // If you want: use data-href later when you have real routes
            const href = slide.getAttribute('data-href') || slide.getAttribute('href');

            // Placeholder now: prevent jump to "#"
            if (!href || href === '#') {
                e.preventDefault();
                // later: window.location.href = realUrl;
            }
        });
    });

    // Initial
    render();
})();
