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
    const TRAVEL_MS = 4_500;

    const GROUP_COUNT = 5;
    const GROUP_STAGGER_MS = 140;

    function rand(min, max) {
        return min + Math.random() * (max - min);
    }

    function shuffleInPlace(arr) {
        for (let i = arr.length - 1; i > 0; i--) {
            const j = Math.floor(Math.random() * (i + 1));
            [arr[i], arr[j]] = [arr[j], arr[i]];
        }
        return arr;
    }

    function shootOne({ startX, startY, endX, endY, size }) {
        const star = document.createElement('img');
        star.src = '/images/svg/space/movingstar.svg';
        star.alt = '';
        star.setAttribute('aria-hidden', 'true');
        star.className = 'moving-star';

        star.style.width = `${size}px`;
        star.style.height = `${size}px`;

        layer.appendChild(star);

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

    function shootGroup() {
        const width = main.clientWidth;
        const height = main.clientHeight;
        const scrollTop = main.scrollTop;

        // Base line: top-right of the current viewport.
        const baseStartX = Math.max(0, width - 10);
        const baseStartY = scrollTop + 10;

        const baseEndX = -160;
        const baseEndY = scrollTop + Math.min(height * 0.6, height - 10);

        // Spread the group of stars more across the top-right (like a cluster).
        // We'll build lanes first, then randomize launch order so it doesn't feel 1-2-3-4-5.
        const lanes = [];
        for (let i = 0; i < GROUP_COUNT; i++) {
            // Wider spread than before.
            const laneOffsetX = i * 42 + rand(-8, 12);
            const laneOffsetY = i * 14 + rand(-6, 14);

            // Slight size variance; don't strictly decrease by index.
            const size = Math.round(rand(26, 52));

            lanes.push({ laneOffsetX, laneOffsetY, size });
        }

        // Randomize emission order (e.g., 4-1-5-2-3).
        shuffleInPlace(lanes);

        for (let i = 0; i < lanes.length; i++) {
            const { laneOffsetX, laneOffsetY, size } = lanes[i];

            // Keep a consistent stagger, with small jitter.
            const delay = i * GROUP_STAGGER_MS + rand(0, 80);

            window.setTimeout(() => {
                shootOne({
                    startX: baseStartX + laneOffsetX,
                    startY: baseStartY + laneOffsetY,
                    endX: baseEndX + laneOffsetX,
                    endY: baseEndY + laneOffsetY,
                    size
                });
            }, delay);
        }
    }

    function scheduleNext(delayMs) {
        window.__spaceMovingStarTimer = window.setTimeout(() => {
            shootGroup();
            scheduleNext(SHOOT_INTERVAL_MS);
        }, delayMs);
    }

    // First group immediately, then every 10s.
    shootGroup();
    scheduleNext(SHOOT_INTERVAL_MS);
})();
