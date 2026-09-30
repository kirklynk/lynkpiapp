let timer;
let timeoutMs = 30000;
let clearScreenSaver = false;

export function init(dotnet) {

    const events = ['mousemove', 'mousedown', 'keydown', 'touchstart', 'scroll'];
    events.forEach(eventName => {
        window.addEventListener(eventName, () => {
            if (clearScreenSaver) {
                dotnet.invokeMethodAsync('toggleScreenSaver');
            }
            clearScreenSaver = false;
            reset();
        }, true);
    });

    reset();

    function reset() {
        clearTimeout(timer);

        timer = setTimeout(() => {

            clearScreenSaver = true;
            dotnet.invokeMethodAsync('toggleScreenSaver');
        }, timeoutMs);
    }
}


