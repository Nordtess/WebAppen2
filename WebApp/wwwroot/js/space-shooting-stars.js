(function () {
    const main = document.querySelector('.app-main');
    const container = document.getElementById('starsContainer');
    if (!main || !container) return;

    const rand = (min, max) => min + Math.random() * (max - min);

    function createShootingStarFromViewportTopRight(viewTop, viewW) {
        const sStar = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        sStar.classList.add('shooting-star', 'is-laser');

        sStar.setAttribute('width', '400');
        sStar.setAttribute('height', '100');
        sStar.setAttribute('viewBox', '0 0 400 100');

        // Spawn: top-right of current viewport
        const startLeft = viewW - 40;
        sStar.style.top = `${viewTop + 20}px`;
        sStar.style.left = `${startLeft}px`;

        const dur = rand(2.5, 4.0);
        sStar.style.animation = `shootingStarGlide ${dur}s linear forwards`;

        // NOTE: Geometry is drawn RIGHT -> LEFT so that after rotate(155deg)
        // the star head leads the motion (top-right -> bottom-left).
        const tailGroup = document.createElementNS('http://www.w3.org/2000/svg', 'g');
        tailGroup.classList.add('shooting-star-tail-group');
        sStar.appendChild(tailGroup);

        // Aura (wide white wedge): narrow at RIGHT near head, wide at LEFT
        const aura = document.createElementNS('http://www.w3.org/2000/svg', 'polygon');
        aura.setAttribute('points', '370,50 20,30 20,70');
        aura.setAttribute('fill', 'rgba(248, 248, 255, 0.40)');
        aura.setAttribute('filter', 'url(#whiteLaserGlow)');
        tailGroup.appendChild(aura);

        // Core (thin white wedge)
        const core = document.createElementNS('http://www.w3.org/2000/svg', 'polygon');
        core.setAttribute('points', '370,50 40,47 40,53');
        core.setAttribute('fill', '#F8F8FF');
        core.setAttribute('filter', 'url(#whiteLaserGlow)');
        tailGroup.appendChild(core);

        // Stardust trailing LEFT of the head
        for (let i = 1; i <= 5; i++) {
            const particle = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
            particle.classList.add('stardust-particle');
            particle.setAttribute('cx', String(370 - (i * 50)));
            particle.setAttribute('cy', String(50 + rand(-6, 6)));
            particle.setAttribute('r', String(1.5 + rand(0, 1.5)));
            particle.style.animationDelay = `${i * 0.1}s`;
            sStar.appendChild(particle);
        }

        // Head star placed at far RIGHT
        const head = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        head.setAttribute('x', '370');
        head.setAttribute('y', '37');
        head.setAttribute('width', '26');
        head.setAttribute('height', '26');
        head.setAttribute('viewBox', '0 0 20 20');
        head.setAttribute('filter', 'url(#whiteLaserGlow)');

        const headUse = document.createElementNS('http://www.w3.org/2000/svg', 'use');
        headUse.setAttribute('href', '#northStarShape');
        head.appendChild(headUse);
        sStar.appendChild(head);

        sStar.addEventListener('animationend', () => sStar.remove());
        container.appendChild(sStar);
    }

    function createShootingStarGroupInViewport() {
        const viewTop = main.scrollTop;
        const viewW = main.clientWidth;

        const groupCount = Math.floor(rand(1, 4));
        for (let i = 0; i < groupCount; i++) {
            window.setTimeout(() => {
                createShootingStarFromViewportTopRight(viewTop, viewW);
            }, i * rand(550, 950));
        }
    }

    window.setTimeout(createShootingStarGroupInViewport, 1200);
    window.setInterval(createShootingStarGroupInViewport, 10000);
})();
