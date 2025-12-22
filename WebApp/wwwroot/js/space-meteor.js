(function () {
    const main = document.querySelector('.app-main');
    if (!main) return;

    // Ensure a layer exists (kept separate from .space-background so it draws above it).
    let layer = document.getElementById('meteorLayer');
    if (!layer) {
        layer = document.createElement('div');
        layer.id = 'meteorLayer';
        layer.setAttribute('aria-hidden', 'true');
        main.insertBefore(layer, main.firstChild);
    }

    // Prevent multiple instances if the script is loaded twice.
    if (window.__spaceMeteorTimer) {
        clearTimeout(window.__spaceMeteorTimer);
        window.__spaceMeteorTimer = null;
    }

    const METEOR_INTERVAL_MS = 10_000;
    const TRAVEL_MS = 6_000;

    function shootMeteor() {
        const width = main.clientWidth;
        const height = main.clientHeight;
        const scrollTop = main.scrollTop;

        // Start from top-right of the *visible* viewport inside app-main.
        const startX = Math.max(0, width - 10);
        const startY = scrollTop + 10;

        // Travel diagonally down-left across the viewport.
        const endX = -260; // enough to fully exit even with wide SVG

        // Slightly less aggressive than "past the bottom" but still a steep diagonal.
        const endY = scrollTop + height * 0.88;

        const meteor = document.createElement('div');
        meteor.className = 'space-meteor';
        meteor.setAttribute('aria-hidden', 'true');

        // Keep the meteor reasonably sized; SVG has width/height set but we'll control via CSS pixels.
        meteor.style.width = '220px';
        meteor.style.height = '92px';

        layer.appendChild(meteor);

        const anim = meteor.animate(
            [
                { transform: `translate3d(${startX}px, ${startY}px, 0)` },
                { transform: `translate3d(${endX}px, ${endY}px, 0)` }
            ],
            {
                duration: TRAVEL_MS,
                easing: 'linear',
                fill: 'forwards'
            }
        );

        anim.addEventListener('finish', () => meteor.remove());
        window.setTimeout(() => meteor.remove(), TRAVEL_MS + 500);
    }

    function scheduleNext(delayMs) {
        window.__spaceMeteorTimer = window.setTimeout(() => {
            shootMeteor();
            scheduleNext(METEOR_INTERVAL_MS);
        }, delayMs);
    }

    // One immediately for testing, then every 10s.
    shootMeteor();
    scheduleNext(METEOR_INTERVAL_MS);
})();
