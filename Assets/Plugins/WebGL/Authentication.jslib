mergeInto(LibraryManager.library, {
    // Navigates only to fixed same-origin authentication routes; no provider tokens cross this bridge.
    LittleColony_AuthNavigate: function (pathPointer) {
        var path = UTF8ToString(pathPointer);
        if (path === "/auth/facebook" || path === "/auth/discord" || path === "/") {
            window.location.assign(path);
        }
    }
});
