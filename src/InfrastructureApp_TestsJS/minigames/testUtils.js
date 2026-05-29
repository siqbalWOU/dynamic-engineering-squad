const path = require("path");
function setupDOM(html) {
    document.body.innerHTML = html;
}

function loadScript(scriptName) {
    const scriptPath = path.resolve(__dirname, "../../InfrastructureApp/wwwroot/js/minigames", scriptName);
    require(scriptPath);
}

function mockFetchJson(data, ok = true, extra = {}) {
    global.fetch = jest.fn().mockResolvedValue({
        ok,
        json: async () => data,
        ...extra
    });
}

function mockAudioController() {
    return {
        play: jest.fn(),
        playIfNeeded: jest.fn(),
        stop: jest.fn(),
        toggleMute: jest.fn(),
        isMuted: jest.fn(),
        isPlaying: jest.fn()
    };
}

async function flushPromises() {
    await Promise.resolve();
    await Promise.resolve();
}

function setMockLocation() {
    const location = { href: "http://localhost/" };
    const realWindow = global.window.__realWindow || global.window;
    const wrappedWindow = Object.create(realWindow);

    Object.defineProperty(wrappedWindow, "__realWindow", {
        value: realWindow,
        configurable: true
    });
    Object.defineProperty(wrappedWindow, "location", {
        value: location,
        writable: true,
        configurable: true
    });

    wrappedWindow.addEventListener = realWindow.addEventListener.bind(realWindow);
    wrappedWindow.removeEventListener = realWindow.removeEventListener.bind(realWindow);
    wrappedWindow.dispatchEvent = realWindow.dispatchEvent.bind(realWindow);
    wrappedWindow.setTimeout = realWindow.setTimeout.bind(realWindow);
    wrappedWindow.clearTimeout = realWindow.clearTimeout.bind(realWindow);
    wrappedWindow.setInterval = realWindow.setInterval.bind(realWindow);
    wrappedWindow.clearInterval = realWindow.clearInterval.bind(realWindow);

    global.window = wrappedWindow;

    return location;
}

module.exports = {
    setupDOM,
    loadScript,
    mockFetchJson,
    mockAudioController,
    flushPromises,
    setMockLocation
};
