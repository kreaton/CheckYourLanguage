// The recorded voices, the saved settings, and the physical keyboard.

// One <audio> element plays every recording. iOS only lets audio start inside a user gesture, but
// once an element has played in one it may play again later.
window.voice = (() => {
    const el = new Audio();
    el.preload = "auto";
    let done = null;
    const cache = new Map();

    function finish(ok) {
        if (done) {
            const resolve = done;
            done = null;
            resolve(ok);
        }
    }

    el.addEventListener("ended", () => finish(true));
    el.addEventListener("error", () => finish(false));

    return {
        // Resolves when the recording has finished (true) or could not be played (false).
        play(url) {
            finish(true);
            return new Promise(resolve => {
                done = resolve;
                el.pause();
                el.src = url;
                const started = el.play();
                if (started) started.catch(() => finish(false));
            });
        },

        stop() {
            el.pause();
            finish(true);
        },

        // Fetches a recording ahead, so it starts without a delay.
        preload(url) {
            if (cache.has(url)) return;
            if (cache.size > 6) cache.delete(cache.keys().next().value);
            const a = new Audio();
            a.preload = "auto";
            a.src = url;
            cache.set(url, a);
        },
    };
})();

window.gameStore = {
    load(key) {
        try { return localStorage.getItem(key); } catch { return null; }
    },
    save(key, value) {
        try { localStorage.setItem(key, value); } catch { /* private mode: the settings are not kept */ }
    },
};

// Forwards Enter, Space, T, 1 and 2 from the physical keyboard to the game.
window.gameKeys = {
    attach(dotnet) {
        this.detach();
        const ignored = e => e.target instanceof Element && e.target.closest("select, input, textarea");
        this.handler = e => {
            if (e.ctrlKey || e.metaKey || e.altKey || ignored(e)) return;
            const key = e.key;
            if (key === "Enter" || key === " " || key === "t" || key === "T" || key === "1" || key === "2") {
                // Also stops Enter and Space from clicking the button that has focus.
                e.preventDefault();
                if (!e.repeat) dotnet.invokeMethodAsync("OnKey", key);
            }
        };
        // Space clicks a focused button on key up, so that is stopped as well.
        this.upHandler = e => {
            if (e.key === " " && !ignored(e)) e.preventDefault();
        };
        document.addEventListener("keydown", this.handler);
        document.addEventListener("keyup", this.upHandler);
    },
    detach() {
        if (this.handler) document.removeEventListener("keydown", this.handler);
        if (this.upHandler) document.removeEventListener("keyup", this.upHandler);
        this.handler = this.upHandler = null;
    },
};
