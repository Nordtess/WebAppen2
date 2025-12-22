(function () {
    const main = document.querySelector('.app-main');
    const bg = document.querySelector('.app-main .space-background');
    const container = document.getElementById('starsContainer');
    if (!main || !bg || !container) return;

    function syncHeight() {
        // scrollHeight includes the full content height (incl. footer inside main)
        const h = Math.max(main.scrollHeight, main.clientHeight);
        bg.style.height = `${h}px`;
        container.style.height = `${h}px`;
    }

    function initStars() {
        if (!container) return;
        container.innerHTML = '';

        syncHeight();
        const hPx = Math.max(main.scrollHeight, main.clientHeight);

        const rand = (min, max) => min + Math.random() * (max - min);

        // More stars than before, but keep them lightweight.
        const STAR_COUNT = 120;
        const sizes = [18, 20, 22, 24, 26, 28, 30, 35];

        for (let i = 0; i < STAR_COUNT; i++) {
            const size = sizes[Math.floor(Math.random() * sizes.length)];
            const star = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
            star.classList.add('laser-star');
            star.setAttribute('width', String(size));
            star.setAttribute('height', String(size));
            star.setAttribute('viewBox', '0 0 20 20');

            star.style.left = `${Math.random() * 100}%`;
            star.style.top = `${Math.random() * hPx}px`;
            star.style.animationDuration = `${rand(3.5, 7.5)}s`;
            star.style.animationDelay = `${rand(0, 6)}s`;

            const use = document.createElementNS('http://www.w3.org/2000/svg', 'use');
            use.setAttribute('href', '#northStarShape');
            star.appendChild(use);

            container.appendChild(star);
        }

        // More shooting stars than before (was 3). Keep original animation.
        const SHOOTING_COUNT = 5;
        for (let i = 0; i < SHOOTING_COUNT; i++) {
            const sStar = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
            sStar.classList.add('shooting-star');
            sStar.setAttribute('width', '200');
            sStar.setAttribute('height', '4');
            sStar.setAttribute('viewBox', '0 0 200 4');

            // Random positions across full scroll height
            sStar.style.top = `${Math.random() * hPx}px`;
            sStar.style.right = `${-10 - Math.random() * 30}%`;

            // Original-ish cadence (+ randomization); fewer animation operations than the new system
            const dur = rand(9, 15);
            sStar.style.animation = `shootingStarAnim ${dur}s linear infinite`;
            sStar.style.animationDelay = `${rand(0, 10)}s`;

            const rect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
            rect.setAttribute('width', '200');
            rect.setAttribute('height', '4');
            rect.setAttribute('fill', 'url(#shootingStarGradient)');
            rect.setAttribute('filter', 'url(#whiteLaserGlow)');
            sStar.appendChild(rect);

            container.appendChild(sStar);
        }
    }

    // Initial
    initStars();

    // Update on resize and whenever the main scroll container changes size.
    window.addEventListener('resize', () => {
        syncHeight();
    });

    // ResizeObserver handles most dynamic height changes.
    if ('ResizeObserver' in window) {
        const ro = new ResizeObserver(() => {
            // Avoid regenerating stars constantly; only keep height in sync.
            syncHeight();
        });
        ro.observe(main);
        const inner = main.querySelector('.main-inner');
        if (inner) ro.observe(inner);
    }

    // Also update after fonts load (can affect layout)
    if (document.fonts && document.fonts.ready) {
        document.fonts.ready.then(() => syncHeight()).catch(() => { });
    }
})();
