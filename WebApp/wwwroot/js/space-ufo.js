(function () {
    const main = document.querySelector('.app-main');
    if (!main) return;

    // Ensure a layer exists.
    let layer = document.getElementById('ufoLayer');
    if (!layer) {
        layer = document.createElement('div');
        layer.id = 'ufoLayer';
        layer.setAttribute('aria-hidden', 'true');
        main.insertBefore(layer, main.firstChild);
    }

    // Prevent multiple instances if the script is loaded twice.
    if (window.__spaceUfoTimer) {
        clearTimeout(window.__spaceUfoTimer);
        window.__spaceUfoTimer = null;
    }

    const UFO_INTERVAL_MS = 10_000;
    const TRAVEL_MS = 6_000;

    function flyUfo() {
        const width = main.clientWidth;
        const height = main.clientHeight;
        const scrollTop = main.scrollTop;

        // Upper-middle-ish of visible viewport.
        const y = scrollTop + height * 0.22;

        // Start slightly off-screen left, end off-screen right.
        const startX = -260;
        const endX = width + 260;

        const ufo = document.createElement('div');
        ufo.className = 'space-ufo';
        ufo.setAttribute('aria-hidden', 'true');

        // Size can be tweaked later.
        ufo.style.width = '180px';
        ufo.style.height = '120px';

        layer.appendChild(ufo);

        const anim = ufo.animate(
            [
                { transform: `translate3d(${startX}px, ${y}px, 0)` },
                { transform: `translate3d(${endX}px, ${y}px, 0)` }
            ],
            {
                duration: TRAVEL_MS,
                easing: 'linear',
                fill: 'forwards'
            }
        );

        anim.addEventListener('finish', () => ufo.remove());
        window.setTimeout(() => ufo.remove(), TRAVEL_MS + 500);
    }

    function scheduleNext(delayMs) {
        window.__spaceUfoTimer = window.setTimeout(() => {
            flyUfo();
            scheduleNext(UFO_INTERVAL_MS);
        }, delayMs);
    }

    // Fire one immediately for testing, then every 10s.
    flyUfo();
    scheduleNext(UFO_INTERVAL_MS);
})();
