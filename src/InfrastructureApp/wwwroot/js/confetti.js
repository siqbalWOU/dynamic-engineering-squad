function launchConfetti() {
    const canvas = document.getElementById('confetti-canvas');
    if (!canvas) return;

    canvas.width = window.innerWidth;
    canvas.height = window.innerHeight;
    canvas.setAttribute('style', 'position:fixed;top:0;left:0;pointer-events:none;z-index:9999;display:block;');

    const ctx = canvas.getContext('2d');
    const COLORS = ['#ff595e', '#ffca3a', '#6a4c93', '#1982c4', '#8ac926', '#ff924c', '#c77dff'];
    const particles = [];

    function randomBetween(a, b) { return a + Math.random() * (b - a); }

    function createParticle() {
        return {
            x: randomBetween(0, canvas.width),
            y: randomBetween(-100, -10),
            size: randomBetween(7, 16),
            color: COLORS[Math.floor(Math.random() * COLORS.length)],
            speedY: randomBetween(3, 7),
            speedX: randomBetween(-2, 2),
            rotation: randomBetween(0, Math.PI * 2),
            rotationSpeed: randomBetween(-0.08, 0.08),
            opacity: 1
        };
    }

    for (let i = 0; i < 200; i++) {
        particles.push(createParticle());
    }

    function update() {
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        for (let i = particles.length - 1; i >= 0; i--) {
            const p = particles[i];
            p.y += p.speedY;
            p.x += p.speedX;
            p.rotation += p.rotationSpeed;
            if (p.y > canvas.height - 60) p.opacity -= 0.03;
            ctx.save();
            ctx.globalAlpha = p.opacity;
            ctx.fillStyle = p.color;
            ctx.beginPath();
            ctx.arc(p.x, p.y, p.size / 2, 0, Math.PI * 2);
            ctx.fill();
            ctx.restore();
            if (p.opacity <= 0 || p.y > canvas.height + 20) particles.splice(i, 1);
        }
        if (particles.length > 0) {
            requestAnimationFrame(update);
        } else {
            canvas.setAttribute('style', 'position:fixed;top:0;left:0;pointer-events:none;z-index:9999;display:none;');
        }
    }

    update();
}
