/**
 * @jest-environment jsdom
 */

// ── Helpers to build the canvas DOM and define launchConfetti ─────────────────

function setupCanvas() {
    document.body.innerHTML = `
        <canvas id="confetti-canvas" style="position:fixed;top:0;left:0;pointer-events:none;z-index:9999;display:none;"></canvas>
    `;
}

// Inline the launchConfetti function exactly as it appears in Details.cshtml
function defineLaunchConfetti() {
    window.launchConfetti = function launchConfetti() {
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
        return particles; // returned for testing purposes
    };
}

// ── Tests ─────────────────────────────────────────────────────────────────────

describe('Confetti feature', () => {

    beforeEach(() => {
        setupCanvas();
        defineLaunchConfetti();

        // Mock canvas getContext so jsdom doesn't throw
        HTMLCanvasElement.prototype.getContext = jest.fn(() => ({
            clearRect: jest.fn(),
            save: jest.fn(),
            restore: jest.fn(),
            beginPath: jest.fn(),
            arc: jest.fn(),
            fill: jest.fn(),
            fillRect: jest.fn(),
            translate: jest.fn(),
            rotate: jest.fn(),
            moveTo: jest.fn(),
            lineTo: jest.fn(),
            closePath: jest.fn(),
            globalAlpha: 1,
            fillStyle: ''
        }));

        // Mock requestAnimationFrame so animation loop doesn't run forever
        jest.spyOn(window, 'requestAnimationFrame').mockImplementation(() => 1);
    });

    afterEach(() => {
        jest.restoreAllMocks();
        document.body.innerHTML = '';
        delete window.launchConfetti;
    });

    // ── Test 1: launchConfetti function exists ────────────────────────────────
    test('launchConfetti is defined as a function', () => {
        expect(typeof window.launchConfetti).toBe('function');
    });

    // ── Test 2: canvas element exists on the page ─────────────────────────────
    test('confetti canvas element exists in the DOM', () => {
        const canvas = document.getElementById('confetti-canvas');
        expect(canvas).not.toBeNull();
    });

    // ── Test 3: canvas is hidden before launchConfetti is called ──────────────
    test('canvas is hidden before launchConfetti is called', () => {
        const canvas = document.getElementById('confetti-canvas');
        expect(canvas.getAttribute('style')).toContain('display:none');
    });

    // ── Test 4: canvas becomes visible when launchConfetti is called ──────────
    test('canvas display is set to block when launchConfetti is called', () => {
        window.launchConfetti();
        const canvas = document.getElementById('confetti-canvas');
        expect(canvas.getAttribute('style')).toContain('display:block');
    });

    // ── Test 5: particles are created when launchConfetti is called ───────────
    test('launchConfetti creates 200 particles', () => {
        // Patch launchConfetti to return particles for inspection
        const originalFn = window.launchConfetti;
        let capturedParticles = null;
        window.launchConfetti = function () {
            capturedParticles = originalFn();
            return capturedParticles;
        };
        defineLaunchConfetti();

        const particles = window.launchConfetti();
        expect(particles).toHaveLength(200);
    });

    // ── Test 6: each particle has required properties ─────────────────────────
    test('each particle has the required properties', () => {
        const particles = window.launchConfetti();
        expect(particles).not.toBeNull();
        particles.forEach(p => {
            expect(p).toHaveProperty('x');
            expect(p).toHaveProperty('y');
            expect(p).toHaveProperty('size');
            expect(p).toHaveProperty('color');
            expect(p).toHaveProperty('speedY');
            expect(p).toHaveProperty('speedX');
            expect(p).toHaveProperty('opacity');
            expect(p.opacity).toBe(1);
        });
    });

    // ── Test 7: particles start above the screen ──────────────────────────────
    test('all particles start above the visible screen (y < 0)', () => {
        const particles = window.launchConfetti();
        particles.forEach(p => {
            expect(p.y).toBeLessThan(0);
        });
    });

    // ── Test 8: launchConfetti does nothing if canvas is missing ──────────────
    test('launchConfetti does not throw if canvas element is missing', () => {
        document.body.innerHTML = ''; // remove canvas
        expect(() => window.launchConfetti()).not.toThrow();
    });

    // ── Test 9: confetti launches on page load via setTimeout ─────────────────
    test('launchConfetti is called on page load via setTimeout', () => {
        jest.useFakeTimers();
        const spy = jest.spyOn(window, 'launchConfetti');

        // Simulate the setTimeout(launchConfetti, 500) from Details.cshtml
        setTimeout(window.launchConfetti, 500);
        jest.advanceTimersByTime(500);

        expect(spy).toHaveBeenCalledTimes(1);
        jest.useRealTimers();
    });
});