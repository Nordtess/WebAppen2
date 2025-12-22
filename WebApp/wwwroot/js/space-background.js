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

    // Deterministic pseudo-random generator (fast, repeatable)
    function mulberry32(seed) {
        return function () {
            let t = (seed += 0x6D2B79F5);
            t = Math.imul(t ^ (t >>> 15), t | 1);
            t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
            return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
        };
    }

    // Stable seed so stars appear in the same positions each load.
    // (Change this number if you ever want a new "sky" layout.)
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
        // twinkle timing is also deterministic now
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

        // Bulk (4-point stars)
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

        // Special/bigger stars (complex path)
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

    function createShootingStar(viewTop, viewH) {
        const sStar = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        sStar.classList.add('shooting-star', 'is-laser');

        // We'll use a slightly bigger canvas so we can include a "head" star and tail.
        // Place origin so the head sits at the right side.
        sStar.setAttribute('width', '260');
        sStar.setAttribute('height', '40');
        sStar.setAttribute('viewBox', '0 0 260 40');

        // Start near upper-right of current viewport
        const padTop = 40;
        const startTop = viewTop + rand(padTop, Math.max(padTop, viewH * 0.35));
        sStar.style.top = `${startTop}px`;
        sStar.style.right = `${-10 - rand(0, 10)}%`;

        // 2-3 seconds across the view for visibility
        const dur = rand(2.0, 3.0);
        sStar.style.animation = `shootingStarAnim ${dur}s linear 1`;

        // Tail
        const rect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
        rect.setAttribute('x', '0');
        rect.setAttribute('y', '18');
        rect.setAttribute('width', '230');
        rect.setAttribute('height', '4');
        rect.setAttribute('fill', 'url(#shootingStarGradient)');
        rect.setAttribute('filter', 'url(#whiteLaserGlow)');
        sStar.appendChild(rect);

        // Head star (4-point) in front of the tail
        const head = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        head.setAttribute('x', '228');
        head.setAttribute('y', '8');
        head.setAttribute('width', '24');
        head.setAttribute('height', '24');
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

        // 3-5 stars, with small delay between them
        const groupCount = Math.floor(3 + Math.random() * 3);
        for (let i = 0; i < groupCount; i++) {
            window.setTimeout(() => {
                createShootingStar(viewTop, viewH);
            }, i * 200);
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
