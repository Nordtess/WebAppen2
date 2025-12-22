(function () {
    const main = document.querySelector('.app-main');
    const bg = document.querySelector('.app-main .space-background');
    const container = document.getElementById('starsContainer');
    if (!main || !bg || !container) return;

    function syncHeight() {
        const h = Math.max(main.scrollHeight, main.clientHeight);
        bg.style.height = `${h}px`;
        container.style.height = `${h}px`;
        return h;
    }

    function mulberry32(seed) {
        return function () {
            let t = (seed += 0x6D2B79F5);
            t = Math.imul(t ^ (t >>> 15), t | 1);
            t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
            return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
        };
    }

    const prng = mulberry32(1337);
    const rand = (min, max) => min + prng() * (max - min);

    function createStar({ size, topPx, leftPct, useId, viewBox }) {
        const star = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        star.classList.add('laser-star');

        star.setAttribute('width', String(size));
        star.setAttribute('height', String(size));
        star.setAttribute('viewBox', viewBox);

        star.style.left = `${leftPct}%`;
        star.style.top = `${topPx}px`;
        star.style.animationDuration = `${rand(3.5, 7.5)}s`;
        star.style.animationDelay = `${rand(0, 6)}s`;

        const use = document.createElementNS('http://www.w3.org/2000/svg', 'use');
        use.setAttribute('href', useId);
        star.appendChild(use);

        container.appendChild(star);
    }

    function initStars() {
        container.innerHTML = '';

        const hPx = syncHeight();

        const STAR_COUNT = 120;
        const sizes = [18, 20, 22, 24, 26, 28, 30, 35];

        for (let i = 0; i < STAR_COUNT; i++) {
            const size = sizes[Math.floor(prng() * sizes.length)];
            createStar({
                size,
                topPx: prng() * hPx,
                leftPct: prng() * 100,
                useId: '#northStarShape',
                viewBox: '0 0 20 20'
            });
        }

        const SPECIAL_COUNT = 10;
        for (let i = 0; i < SPECIAL_COUNT; i++) {
            createStar({
                size: Math.floor(rand(52, 92)),
                topPx: prng() * hPx,
                leftPct: prng() * 100,
                useId: '#geminiStarShape',
                viewBox: '155.8 111.4 407.4 407.6'
            });
        }
    }

    function createShootingStarFromTopRight(viewTop, viewH) {
        const sStar = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        sStar.classList.add('shooting-star', 'is-laser');

        sStar.setAttribute('width', '280');
        sStar.setAttribute('height', '44');
        sStar.setAttribute('viewBox', '0 0 280 44');

        // Start near the upper-right corner of the current viewport
        // Keep it consistently in the "yellow-line" region (top 25% of the viewport)
        const startTop = viewTop + rand(10, Math.max(10, viewH * 0.14));
        sStar.style.top = `${startTop}px`;

        // Position just off the right edge so it enters from the corner
        sStar.style.right = `${-6 - rand(0, 4)}%`;

        // Slow travel (2–3s) - previously too fast
        const dur = rand(5.2, 6.8);
        sStar.style.animation = `shootingStarAnim ${dur}s linear 1`;

        const rect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
        rect.setAttribute('x', '0');
        rect.setAttribute('y', '20');
        rect.setAttribute('width', '250');
        rect.setAttribute('height', '4');
        rect.setAttribute('fill', 'url(#shootingStarGradient)');
        rect.setAttribute('filter', 'url(#whiteLaserGlow)');
        sStar.appendChild(rect);

        const head = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        head.setAttribute('x', '248');
        head.setAttribute('y', '10');
        head.setAttribute('width', '26');
        head.setAttribute('height', '26');
        head.setAttribute('viewBox', '0 0 20 20');
        head.setAttribute('filter', 'url(#whiteLaserGlow)');

        const headUse = document.createElementNS('http://www.w3.org/2000/svg', 'use');
        headUse.setAttribute('href', '#northStarShape');
        head.appendChild(headUse);
        sStar.appendChild(head);

        sStar.addEventListener('animationend', () => {
            sStar.remove();
        });

        container.appendChild(sStar);
    }

    function createShootingStarGroupInViewport() {
        syncHeight();
        const viewTop = main.scrollTop;
        const viewH = main.clientHeight;

        const groupCount = Math.floor(rand(3, 6));
        for (let i = 0; i < groupCount; i++) {
            window.setTimeout(() => {
                createShootingStarFromTopRight(viewTop, viewH);
            }, i * 220);
        }
    }

    function startShootingStarGroups() {
        window.setTimeout(createShootingStarGroupInViewport, 1200);
        window.setInterval(() => {
            createShootingStarGroupInViewport();
        }, 10000);
    }

    initStars();
    startShootingStarGroups();

    window.addEventListener('resize', () => {
        syncHeight();
    });

    if ('ResizeObserver' in window) {
        const ro = new ResizeObserver(() => {
            syncHeight();
        });
        ro.observe(main);
        const inner = main.querySelector('.main-inner');
        if (inner) ro.observe(inner);
    }

    if (document.fonts && document.fonts.ready) {
        document.fonts.ready.then(() => syncHeight()).catch(() => { });
    }
})();
