(function () {
    const main = document.querySelector('.app-main');
    const layer = document.getElementById('shootingStarsLayer');
    if (!main || !layer) return;

    // Prevent multiple instances if the script is loaded twice.
    if (window.__spaceMovingStarTimer) {
        clearTimeout(window.__spaceMovingStarTimer);
        window.__spaceMovingStarTimer = null;
    }

    const SHOOT_INTERVAL_MS = 10_000;
    const TRAVEL_MS = 3_500;

    function shootOnce() {
        const width = main.clientWidth;
        const height = main.clientHeight;
        const scrollTop = main.scrollTop;

        const startX = Math.max(0, width - 10);
        const startY = scrollTop + 10;

        const endX = -120;
        const endY = scrollTop + Math.min(height * 0.6, height - 10);

        const star = document.createElement('img');
        star.src = '/images/svg/space/movingstar.svg';
        star.alt = '';
        star.setAttribute('aria-hidden', 'true');
        star.className = 'moving-star';

        const size = 42;
        star.style.width = `${size}px`;
        star.style.height = `${size}px`;

        layer.appendChild(star);

        // Animate via WAAPI for smoother compositor-driven transforms.
        const anim = star.animate(
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

        anim.addEventListener('finish', () => {
            star.remove();
        });

        // Fallback cleanup in case finish doesn't fire.
        window.setTimeout(() => {
            star.remove();
        }, TRAVEL_MS + 250);
    }

    function scheduleNext(delayMs) {
        window.__spaceMovingStarTimer = window.setTimeout(() => {
            shootOnce();
            scheduleNext(SHOOT_INTERVAL_MS);
        }, delayMs);
    }

    // First one immediately, then every 10s.
    shootOnce();
    scheduleNext(SHOOT_INTERVAL_MS);
})();
